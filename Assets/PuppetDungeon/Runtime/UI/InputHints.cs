using UnityEngine;
using UnityEngine.InputSystem;

namespace PuppetDungeon
{
    /// <summary>
    /// Dernier périphérique utilisé (clavier/souris ou manette) et libellés des touches correspondants,
    /// pour que les aides affichées correspondent à ce que le joueur tient en main.
    /// </summary>
    public static class InputHints
    {
        private static int frame = -1;
        private static bool gamepad;

        public static bool UsingGamepad
        {
            get { Poll(); return gamepad; }
        }

        private static void Poll()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            var g = Gamepad.current;
            if (g != null && GamepadActive(g)) { gamepad = true; return; }
            var k = Keyboard.current;
            var m = Mouse.current;
            if ((k != null && k.anyKey.wasPressedThisFrame)
                || (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame || m.delta.ReadValue().sqrMagnitude > 25f)))
                gamepad = false;
        }

        private static bool GamepadActive(Gamepad g)
        {
            return g.leftStick.ReadValue().sqrMagnitude > 0.16f || g.rightStick.ReadValue().sqrMagnitude > 0.16f
                || g.dpad.ReadValue().sqrMagnitude > 0.16f
                || g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame
                || g.startButton.wasPressedThisFrame || g.selectButton.wasPressedThisFrame
                || g.leftShoulder.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame
                || g.leftTrigger.ReadValue() > 0.4f || g.rightTrigger.ReadValue() > 0.4f;
        }

        // Libellés (disposition Xbox, la plus répandue sur PC).
        public static string Attack => UsingGamepad ? "X / RT" : "Clic / J";
        public static string Dash => UsingGamepad ? "A / RB" : "Espace";
        public static string Interact => UsingGamepad ? "Y" : "E";
        public static string Inventory => UsingGamepad ? "View" : "I / Tab";
        public static string Pause => UsingGamepad ? "Menu" : "Échap";

        /// <summary>Aide des touches du HUD.</summary>
        public static string HudHelp()
        {
            const string c = "<color=#F2C14E>";
            return UsingGamepad
                ? $"{c}{Attack}</color> attaquer   {c}{Dash}</color> ruée   {c}Stick droit</color> viser   {c}{Inventory}</color> pantin   {c}{Pause}</color> pause"
                : $"{c}{Attack}</color> attaquer   {c}{Dash}</color> ruée   {c}{Inventory}</color> pantin   {c}{Pause}</color> pause";
        }
    }
}
