using UnityEngine;

namespace RovitNacional
{
    public enum Mode
    {
        TEA,
        Down,
        AC
    }

    public enum Activity
    {
        BuldingTower,
        GoStopGo
    }

    public static class Experimento
    {
        private static Activity _actividad;
        private static Mode _modo;

        public static Activity Actividad
        {
            get => _actividad;
            set => _actividad = value;
        }

        public  static Mode Modo
        {
            get => _modo;
            set => _modo = value;
        }
        
    }
}