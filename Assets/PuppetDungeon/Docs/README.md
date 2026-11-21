# Les Fils du Pantin — Puppet Dungeon (Volcano Game Jam 3)

Dungeon crawler en vue de dessus (caméra « diorama » façon *Link's Awakening* sur Switch), salles procédurales,
pantin de bois personnalisable pièce par pièce (inventaire « à la Diablo »), progression roguelike avec
renaissance au **village des marionnettes** tous les 5 donjons.

---

## 1. Démarrage rapide (Unity 6.6 — 6000.6.0f1)

1. Ouvrez le projet dans Unity.
2. Menu **Tools > Puppet Dungeon > 1. Setup complet (tout construire)**.
   - Si l'Universal Render Pipeline (URP) n'est pas installé, l'outil l'installe via le Package Manager.
     Le setup **reprend tout seul** après la recompilation (environ 1 minute).
   - L'outil règle ensuite le projet et génère tout : calques, import 9-slice des panneaux bois, rig humanoïde,
     clips d'animation, matériaux URP, Animator, effets de particules, prefabs (joueur, villageois, ennemis, boss,
     projectiles, butin), pièces d'exemple à partir de vos FBX, interface complète, profil de post-process
     et les 3 scènes (menu, donjon, village) ajoutées aux Build Settings.
3. **Tools > Puppet Dungeon > 2. Jouer** (ou ouvrez `Generated/Scenes/PD_MainMenu` et appuyez sur Play).

Vous pouvez relancer le setup à tout moment : les **contenus** (`Content/` : pièces, ennemis, thèmes) et la
**configuration** (`Resources/PD_GameConfig`) sont conservés ; seuls les fichiers de `Generated/` sont reconstruits.

## 2. Commandes

| Action | Clavier / souris | Manette |
|---|---|---|
| Se déplacer | WASD (ZQSD en AZERTY) / flèches | Stick gauche / croix |
| Attaquer (vise la souris) | Clic gauche / J | Carré-X / RT |
| Ruée (invulnérable) | Espace / Maj / clic droit | A-Croix / RB |
| Interagir | E | Y-Triangle |
| Pantin (inventaire) | Tab / I | Select |
| Pause | Échap | Start |

Inventaire : **glisser-déposer** une pièce sur l'emplacement du pantin pour l'équiper, **clic droit** (ou double clic)
pour équiper/retirer, glisser dans la **corbeille** pour la détruire. Le survol affiche l'info-bulle avec
la comparaison face à la pièce portée. Le jeu est en pause tant que l'inventaire est ouvert.

## 3. Ce qui est en place (design)

### Donjons et salles
- Génération **à la demande** (repris de `ElGenerator`, corrigé) : chaque porte non liée génère la salle suivante,
  collée porte contre porte ; la salle d'arrivée reçoit une **porte d'entrée derrière le joueur**.
- Chaque donjon compte N salles (augmente avec la profondeur), puis apparaît la **salle de sortie** (trappe).
- Salles de **boss** aléatoires (plus grandes) ; la sortie peut avoir un **gardien**.
- **Zone de sûreté** (anneau bleu) devant la porte d'entrée : tant que le joueur y reste, les ennemis dorment
  (ils se balancent au bout de leurs fils) et le joueur peut faire demi-tour. S'il sort de la zone (ou frappe un
  ennemi), **les portes se ferment** (barreaux qui tombent) jusqu'à ce que la salle soit nettoyée.
- **Caméra** : cadre toujours toute la salle, le reste est **noir profond** ; glissement à la Zelda d'une salle à
  l'autre ; suit le joueur si une salle déborde de l'écran. Tilt-shift (effet maquette) piloté par le post-process.

### Pantin, pièces et progression
- 6 emplacements : tête, torse, bras gauche/droit, jambe gauche/droite. Une pièce équipée **remplace le segment**
  correspondant du pantin de base (accroché à l'os humanoïde, il suit les animations).
- **Bras droit = style d'attaque** : Poing, Lame (combo de 3 coups), Arbalète (tir), Bouclier (gros recul + garde).
- **Raretés** (Commun → Légendaire) : multiplicateur de stats, stats supplémentaires, **passifs** à partir de Rare
  (Échardes, Sève, Ronces, Second souffle, Frénésie, Élan, Bourreau, Vernis, Fil tranchant, Porte-bonheur).
- La puissance des pièces et des ennemis **croît de façon exponentielle** avec la profondeur, et encore plus à
  chaque cycle de village (`cycleDifficultyMultiplier`).
- Chaque pièce a une **histoire** (affichée dans l'info-bulle).

### Village des marionnettes (tous les 5 donjons)
Le pantin arrive brisé (terne, immobilisé). Une marionnette vient à sa rencontre et lui parle ; le joueur
choisit **une** pièce à offrir (ou **plusieurs**, rarement : « Nuit des Fils Dorés »). La marionnette devient le
**nouveau personnage joueur** (stats de base ×1,35, nouveau nom, nouvelle teinte) ; le vieux pantin reste au
village. La porte du nord mène aux donjons suivants.

### Juice
Hit-stop, secousses de caméra (trauma), coups directionnels, flash blanc, squash & stretch, recul, chiffres de
dégâts (critiques dorés), ralenti sur le dernier ennemi, pantins qui **se disloquent en débris physiques**,
copeaux de bois, poussière, barreaux qui claquent, butin qui jaillit avec un faisceau à la couleur de sa rareté,
vignette rouge et aberration chromatique quand on est touché, alerte à PV bas, bannières et cartes de titre.

## 4. Architecture

```
Assets/PuppetDungeon/
  Runtime/            (assembly PuppetDungeon.Runtime, namespace PuppetDungeon)
    Core/             GameManager (run, scènes, pause/hit-stop/ralenti, fondu), RunState + Inventory, Settings
    Data/             Enums, Stats, GameConfig, EnemyDefinition, DungeonTheme, PartDatabase, VfxLibrary, AudioLibrary
    Items/            PartDefinition (contenu), PartInstance (pièce tirée), LootGenerator, PartVisual, PartPreview
    Gameplay/         PlayerController, PlayerCombat, Health, PuppetRig, Projectile, LootPickup, Interactable
    Enemies/          EnemyController (machine à états : endormi → alerte → poursuite → armement → attaque)
    Dungeon/          DungeonManager (génération + transitions), RoomBuilder (géométrie), RoomController,
                      Door, ExitStairs, MaterialFactory
    Camera/           RoomCamera (cadrage + secousses), PostFX (Volume URP dynamique)
    FX/               Juice (boîte à outils), HitFlash, SquashStretch, DamageNumber, TelegraphCircle…
    UI/               UIRoot, HUD, Inventaire (glisser-déposer), info-bulle, pause, options, game over,
                      offrande, dialogue, rendu automatique des icônes (PartIconRenderer)
    Village/          VillageDirector, VillagerNPC, VillageDecor, MenuDiorama
  Editor/             (assembly PuppetDungeon.Editor) outil de setup + assistant de création de pièce
  Input/              PD_Controls.inputactions (Input System)
  Content/            ✎ VOS données : pièces, ennemis, thèmes, base de pièces
  Resources/          ✎ PD_GameConfig (équilibrage global)
  Generated/          ⚙ tout ce que l'outil reconstruit (prefabs, UI, matériaux, scènes…)
```

Principes : données dans des **ScriptableObjects**, outils Unity natifs (CharacterController, Input System,
uGUI + TextMeshPro, Animator humanoïde + masque d'avatar, ParticleSystem, URP Volumes, Package Manager,
AssetPostprocessor). Les UI et prefabs sont **construits par script** (aucune manipulation manuelle).
URP est optionnel à la compilation (`versionDefines` → `PD_URP`) pour que l'outil puisse l'installer lui-même.

## 5. Ajouter du contenu

### Une nouvelle pièce (le plus simple)
**Tools > Puppet Dungeon > Créer une pièce depuis un modèle…**
1. Glissez un FBX/prefab modélisé **dans le repère du pantin de base** (`rig_ff002`), comme `bras.fbx` ou `mode_*.fbx`.
2. L'assistant détecte les emplacements couverts ; choisissez l'emplacement (symétrie gauche/droite automatique).
3. Nom, style d'attaque (bras droit), stats de base (valeurs au niveau 1 en Commun), passifs possibles, histoire.
4. **Créer** : chaque maillage est accroché à l'os le plus proche, le visuel `PV_*.prefab` et la définition
   `PD_Part_*.asset` sont créés et ajoutés à la base de pièces. L'icône d'inventaire est rendue automatiquement.

À la main : *Create > Puppet Dungeon > Part Definition* (toute nouvelle définition est ajoutée automatiquement à
`Content/PD_PartDatabase`).

### Un ennemi / un boss
Dupliquez un `Content/Enemies/PD_Enemy_*.asset`, réglez ses stats et son comportement (Chaser, Shooter, Charger, Boss),
puis ajoutez-le aux listes `enemies` ou `bosses` de `PD_GameConfig`.

### Un thème de donjon
Dupliquez un `Content/Themes/PD_Theme_*.asset` (couleurs du sol, des murs, lumière, lanternes) et ajoutez-le à `themes`.

### Sons et musiques
`Generated/Data/PD_AudioLibrary` : chaque emplacement accepte plusieurs clips (tirés au hasard, hauteur variée).
Avec les 3 fichiers fournis, l'outil décline `hit-wood` et `wood-tap-click` par la hauteur pour couvrir tous les
sons ; remplacez-les simplement par de nouveaux clips. Musiques : `musicDungeon`, `musicCombat`, `musicBoss`,
`musicVillage`, `musicMenu` (vides → la musique du donjon est utilisée).

## 6. Équilibrage (`Resources/PD_GameConfig`)
- `dungeonsPerVillage` (5), `roomsPerDungeon`, `extraRoomsPerDungeon`, tailles de salle, `bossRoomChance`.
- `enemyHealthGrowth`, `enemyDamageGrowth` (par donjon), `cycleDifficultyMultiplier` (par village).
- `rarityWeights`, `rarityShiftPerDungeon`, `rarityStatMultiplier`, `rarityExtraStats`, `rarityPassiveCount`, `itemPowerGrowth`.
- `playerBase` (stats du pantin nu), `rebirthBaseGrowth`, `inventorySize`, `multiOfferingChance`, `starterParts`.

## 7. Ancien code
Les scripts du prototype (`ElGenerator`, `PorteScript`, `PlayerManager`, `Part`, `Enemy`…) ont servi de base
(génération porte contre porte, zone de sûreté, stats de pièces, accroche aux os) et ont été réécrits.
Dans le ZIP, ils sont conservés dans `Assets/Projet/_Legacy~/` (dossier ignoré par Unity) car certains importaient
`UnityEditor`/`NUnit` dans du code de jeu, ce qui empêchait de compiler un exécutable. Les anciennes scènes de test
peuvent donc afficher des « Missing Script ».

## 8. Pistes
- Modéliser des têtes/torses dédiés (le système les accepte déjà).
- Ajouter des clips d'animation (coup reçu, mort, ruée) : brancher dans `PD_Puppet.controller`.
- Plus de types de salles (trésor, piège, défi) en étendant `RoomKind` et `RoomController`.
