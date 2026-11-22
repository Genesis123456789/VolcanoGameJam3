# Puppet Dungeon (Volcano Game Jam 3)

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
- Le type de chaque **nouvelle** salle est tiré selon le nombre de salles déjà parcourues (courbes éditables
  dans `PD_GameConfig`, avec une table de simulation dans l'inspecteur) : **plus on explore, plus la sortie est
  probable** ; **les boss sont rares en début de donjon** (et limités à `maxBossRoomsPerDungeon`).
  Filet de sécurité : sortie garantie après `maxRoomsBeforeExit` salles, et une nouvelle sortie peut réapparaître
  si la précédente a été laissée derrière (on ne peut plus errer 70 salles sans fin).
- La salle de sortie s'annonce (bannière, bornes lumineuses, étincelles) ; elle peut avoir un **gardien**.
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

**Gacha** : toutes les pièces **non offertes** sont recyclées en **Bobines** (valeur selon la rareté et le niveau,
réglable : `rarityCurrencyValue`, `currencyPerItemLevel`). L'écran d'offrande affiche le gain à venir.
**La Tisseuse de Fils** (échoppe à l'ouest de la place) échange les Bobines contre des tirages x1 ou x5
(probabilités affichées, garantie Épique tous les `gachaPity` tirages, prix croissant à chaque cycle).
Les pièces tirées vont dans le sac (sac plein → recyclées, une partie du prix est rendue).

### Juice
Hit-stop, secousses de caméra (trauma), coups directionnels, flash blanc, squash & stretch, recul, chiffres de
dégâts (critiques dorés), ralenti sur le dernier ennemi, pantins qui **se disloquent en débris physiques**,
copeaux de bois, poussière, barreaux qui claquent, butin qui jaillit avec un faisceau à la couleur de sa rareté,
vignette rouge et aberration chromatique quand on est touché, alerte à PV bas, bannières et cartes de titre.

### Animations (Humanoid)
Tous les pantins (joueur, ennemis, villageois) partagent **un seul Animator** (`PD_Puppet.controller`) :
- calque de base : **Idle → Marche → Course** (blend tree sur `Speed`) et **attaque à l'arrêt en corps entier** ;
- calque **HautDuCorps** (masque d'avatar) : **attaque en se déplaçant**, jouée par-dessus la marche/course
  (mélange attaque + marche), **garde/visée** en combat à l'arrêt, **célébration** quand une salle est nettoyée.

Chaque arme a son **jeu d'animations** (`Content/Animation/PD_Anim_*.asset`) qui génère un
**AnimatorOverrideController** : changer d'arme au bras droit change les attaques.

| Jeu | Attaque (en marchant) | Attaque (à l'arrêt) | Garde |
|---|---|---|---|
| Poings (défaut) | hit_Shilder | hit_Shilder | garde_pose |
| Lame | sworld_att | sworld_att | garde_pose |
| Arbalète | shoot | sot_genoux (tir à genou) | aim_pose |
| Bouclier | hit_Shilder | hit_Shilder | garde_pose |

Idle = `idle`, marche = `walk_cicle`, course = `run_cicle`, célébration = `67`. Les ennemis utilisent le jeu de
leur arme (Errant : lame, Guetteur : arbalète, Rempart : bouclier).

**Pourquoi une « cuisson »** : les FBX d'animation ont été exportés **sans « Bake Animation »** : seuls les
contrôleurs Maya (`nurbsCircle…`) ont des clés, les articulations n'en ont aucune (les contraintes et l'IK Maya ne
passent pas en FBX). Unity n'avait donc rien à jouer — d'où le pantin figé. **Vos FBX ne sont pas modifiés** :
l'outil (`Tools > Puppet Dungeon > Animations > Cuire toutes les animations`, lancé aussi par le setup)
1. lit les courbes des contrôleurs (import Generic) ;
2. rejoue les contraintes décrites dans `Content/Animation/PD_RigProfile_Maya` : orientation des bras, tête et
   colonne, contrainte parent des hanches, **IK des jambes avec pole vector**. Les décalages sont mesurés dans la
   pose exportée ; ils sont identiques d'un fichier à l'autre (vérifié sur les 12 fichiers), et l'IK reproduit
   exactement la position des genoux exportée. Le point visé par chaque pole vector est calibré automatiquement
   (le contrôleur `genoux_R`, qui pilote la jambe **gauche**, a un pivot décalé) ;
3. convertit chaque image en pose humanoïde (`HumanPoseHandler`) → `Generated/Animation/Baked/PD_<nom>.anim`.

Les avatars humanoïdes sont construits avec `AvatarBuilder` (`Generated/Animation/Avatars`). Le rig
`rig_ff002` nomme « LeftUpLeg » la jambe située à droite du personnage : l'outil corrige le côté géométriquement.

**Conseil Maya** : à l'export FBX, cochez *Animation > Bake Animation* (articulations). Les articulations déjà
animées sont reprises telles quelles par l'outil, sans profil.

### Pièces : alignement sur le pantin
Les pièces (`mode_*.fbx`, `bras.fbx`, `jambe.fbx`) sont modélisées sur le corps de référence
`mode_perso_en_vrais.fbx` (pieds à y = 0), alors que le pantin riggé est ~5,2 unités plus haut (bras +5,5,
tête +5,0) : elles s'accrochaient trop bas (jambes décollées, bras flottants). Le décalage est maintenant mesuré
**os par os** en comparant les segments du corps de référence (nommés comme les os) à ceux du rig.

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
    Village/          VillageDirector, VillagerNPC, VillageDecor, MenuDiorama, Gacha
    Animation/        PuppetAnimationSet (jeu par arme), AnimationRigProfile, AnimationLibrary
  Editor/             (assembly PuppetDungeon.Editor) outil de setup, cuisson des animations (PDAnimBaker),
                      avatars (PDAvatarTools), Animator + jeux (PDAnimSets), assistant de pièce, inspecteur de config
  Input/              PD_Controls.inputactions (Input System)
  Content/            ✎ VOS données : pièces, ennemis, thèmes, base de pièces, Animation/ (jeux, profil,
                      bibliothèque), PD_Credits
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

### Une animation
1. Déposez le FBX dans `Assets/Projet/Animation`.
2. `Tools > Puppet Dungeon > Animations > Cuire toutes les animations` : il est ajouté à
   `Content/Animation/PD_AnimationLibrary` (cochez/décochez *Loop*, renommez le clip si besoin) et cuit en
   `PD_<nom>.anim`.
3. Glissez le clip dans un jeu d'animations (`PD_Anim_*.asset`) puis relancez le setup (ou
   *Create > Puppet Dungeon > Animation > Jeu d'animations* pour une nouvelle arme, et associez-le à un style
   dans `PD_GameConfig > styleAnimations`).

### Un nouveau rig (ex. ennemi différent)
- Rig humanoïde avec des noms usuels (Hips, Spine, LeftArm, LeftForeArm, LeftUpLeg…) : rien à faire, l'avatar
  est construit automatiquement ; sinon renseignez *Bone Names* dans un profil de rig.
- Animations exportées sans « Bake » : dupliquez `PD_RigProfile_Maya`, décrivez les contrôleurs (chemin
  `GROUPE/contrôleur`, espaces de noms ignorés) et l'IK, puis choisissez ce profil dans l'entrée de la bibliothèque.
- Les clips Humanoid se réciblent sur n'importe quel pantin humanoïde.

### Les crédits
`Content/PD_Credits.asset` : titre, sections (titre + lignes, texte riche accepté), message de fin, vitesse
de défilement, musique. Bouton **Crédits** du menu principal.

### Un ennemi / un boss
Dupliquez un `Content/Enemies/PD_Enemy_*.asset`, réglez ses stats et son comportement (Chaser, Shooter, Charger, Boss),
puis ajoutez-le aux listes `enemies` ou `bosses` de `PD_GameConfig`. Son champ `animations` choisit ses attaques.

### Un thème de donjon
Dupliquez un `Content/Themes/PD_Theme_*.asset` (couleurs du sol, des murs, lumière, lanternes) et ajoutez-le à `themes`.

### Sons et musiques
`Generated/Data/PD_AudioLibrary` : chaque emplacement accepte plusieurs clips (tirés au hasard, hauteur variée).
Avec les 3 fichiers fournis, l'outil décline `hit-wood` et `wood-tap-click` par la hauteur pour couvrir tous les
sons ; remplacez-les simplement par de nouveaux clips. Musiques : `musicDungeon`, `musicCombat`, `musicBoss`,
`musicVillage`, `musicMenu` (vides → la musique du donjon est utilisée).

## 6. Équilibrage (`Resources/PD_GameConfig`)
- `dungeonsPerVillage` (5), tailles de salle, courbes `exitChanceByRooms` / `bossChanceByRooms`,
  `maxRoomsBeforeExit`, `roomsBeforeAnotherExit`, `maxBossRoomsPerDungeon`, `extraRoomsPerDungeon` (décale les courbes).
- Gacha : `currencyName`, `rarityCurrencyValue`, `gachaCost`, `gachaMultiCount`, `gachaRarityWeights`, `gachaPity`.
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
- Ajouter des clips d'animation (coup reçu, mort, ruée) : nouvel emplacement dans `PDAnimSets`.
- `Grasses_Asset.fbx` (dossier `mode/asset`) est vide (aucun objet dans le fichier) : à réexporter.
- Plus de types de salles (trésor, piège, défi) en étendant `RoomKind` et `RoomController`.
