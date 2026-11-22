namespace PuppetDungeon
{
    /// <summary>Identité du jeu (titre affiché, crédits, nom du build).</summary>
    public static class GameInfo
    {
        public const string Title = "Puppet Dungeon";

        /// <summary>Ancien titre de travail : remplacé automatiquement là où il est encore enregistré.</summary>
        public const string LegacyTitle = "Les Fils du Pantin";

        /// <summary>Titre à afficher : le titre stocké, sauf s'il est vide ou encore l'ancien titre.</summary>
        public static string Resolve(string stored)
            => string.IsNullOrWhiteSpace(stored) || string.Equals(stored.Trim(), LegacyTitle, System.StringComparison.OrdinalIgnoreCase) ? Title : stored;
    }
}
