using UnityEngine;

public class Constants
{
    public static class PlayerData
    {
        public static class PlayerStats
        {
            public static float maxHealth = 100f;
            public static float maxMana = 50f;
            public static float moveSpeed = 5f;
            public static float jumpForce = 12f;
            public static float attackDamage = 15f;
            public static float attackCooldown = 0.3f;
        }

        public static class PlayerControls
        {
            public static KeyCode down = KeyCode.DownArrow;
            public static KeyCode up = KeyCode.UpArrow;
            public static KeyCode left = KeyCode.LeftArrow;
            public static KeyCode right = KeyCode.RightArrow;

            public static KeyCode jump = KeyCode.Z;
            public static KeyCode attack = KeyCode.X;
            public static KeyCode dash = KeyCode.C;
            public static KeyCode interact = KeyCode.UpArrow;
        }
    }
}
