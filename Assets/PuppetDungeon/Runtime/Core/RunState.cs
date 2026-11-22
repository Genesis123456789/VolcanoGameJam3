using System;
using System.Collections.Generic;
using UnityEngine;

namespace PuppetDungeon
{
    /// <summary>
    /// Sac + emplacements du pantin. Modèle pur (aucune UI) : l'inventaire graphique s'abonne à Changed.
    /// </summary>
    [Serializable]
    public class Inventory
    {
        public PartInstance[] bag;
        public PartInstance[] equipped = new PartInstance[6];

        public event Action Changed;

        public Inventory(int size)
        {
            bag = new PartInstance[size];
        }

        public int Size => bag.Length;

        public PartInstance GetEquipped(PuppetSlot s) => equipped[(int)s];

        public IEnumerable<PartInstance> AllEquipped()
        {
            foreach (var p in equipped) if (p != null) yield return p;
        }

        public IEnumerable<PartInstance> AllOwned()
        {
            foreach (var p in equipped) if (p != null) yield return p;
            foreach (var p in bag) if (p != null) yield return p;
        }

        public int FreeCells
        {
            get
            {
                int n = 0;
                foreach (var p in bag) if (p == null) n++;
                return n;
            }
        }

        public bool TryAdd(PartInstance part)
        {
            if (part == null) return false;
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] != null) continue;
                bag[i] = part;
                Notify();
                return true;
            }
            return false;
        }

        /// <summary>Échange deux cases du sac.</summary>
        public void MoveInBag(int from, int to)
        {
            if (!Valid(from) || !Valid(to) || from == to) return;
            var tmp = bag[to];
            bag[to] = bag[from];
            bag[from] = tmp;
            Notify();
        }

        /// <summary>Équipe la pièce d'une case du sac. L'ancienne pièce prend sa place dans le sac.</summary>
        public bool EquipFromBag(int bagIndex)
        {
            if (!Valid(bagIndex) || bag[bagIndex] == null) return false;
            var part = bag[bagIndex];
            int s = (int)part.Slot;
            bag[bagIndex] = equipped[s];
            equipped[s] = part;
            Notify();
            return true;
        }

        /// <summary>Retire une pièce équipée vers une case (ou la première case libre si -1).</summary>
        public bool Unequip(PuppetSlot slot, int toBagIndex = -1)
        {
            var part = equipped[(int)slot];
            if (part == null) return false;
            if (toBagIndex >= 0 && Valid(toBagIndex))
            {
                var other = bag[toBagIndex];
                if (other != null && other.Slot != slot)
                {
                    // La case cible contient une pièce d'un autre emplacement : on cherche une case libre.
                    toBagIndex = -1;
                }
                else
                {
                    bag[toBagIndex] = part;
                    equipped[(int)slot] = other; // échange si même emplacement
                    Notify();
                    return true;
                }
            }
            for (int i = 0; i < bag.Length; i++)
            {
                if (bag[i] != null) continue;
                bag[i] = part;
                equipped[(int)slot] = null;
                Notify();
                return true;
            }
            return false;
        }

        public void SetEquipped(PuppetSlot slot, PartInstance part)
        {
            equipped[(int)slot] = part;
            Notify();
        }

        public PartInstance RemoveFromBag(int index)
        {
            if (!Valid(index)) return null;
            var p = bag[index];
            bag[index] = null;
            Notify();
            return p;
        }

        public bool Remove(PartInstance part)
        {
            for (int i = 0; i < equipped.Length; i++)
                if (equipped[i] == part) { equipped[i] = null; Notify(); return true; }
            for (int i = 0; i < bag.Length; i++)
                if (bag[i] == part) { bag[i] = null; Notify(); return true; }
            return false;
        }

        public void Clear()
        {
            for (int i = 0; i < bag.Length; i++) bag[i] = null;
            for (int i = 0; i < equipped.Length; i++) equipped[i] = null;
            Notify();
        }

        public void ClearBag()
        {
            for (int i = 0; i < bag.Length; i++) bag[i] = null;
            Notify();
        }

        private bool Valid(int i) => i >= 0 && i < bag.Length;

        public void Notify() => Changed?.Invoke();
    }

    /// <summary>État de la run (survit aux changements de scène grâce au GameManager).</summary>
    [Serializable]
    public class RunState
    {
        public int dungeonIndex;            // nombre de donjons terminés (0 au début)
        public int generation = 1;          // numéro du pantin courant
        public string puppetName = "Brindille";
        public float baseScale = 1f;        // croissance des stats de base à chaque renaissance
        public Color woodTint = new Color(0.86f, 0.70f, 0.50f);
        public float currentHealth = -1f;   // -1 = plein
        public int roomsCleared;
        public int enemiesKilled;
        public int bossesKilled;
        public float playTime;
        public List<string> fallenPuppets = new List<string>();
        public int currency;                // monnaie du gacha (pièces recyclées au village)
        public int gachaSincePity;          // tirages depuis la dernière pièce Épique ou mieux
        public int gachaPulls;
        public int partsRecycled;
        public Inventory inventory;

        public RunState(int inventorySize)
        {
            inventory = new Inventory(inventorySize);
        }
    }
}
