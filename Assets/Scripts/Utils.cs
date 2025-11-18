using UnityEngine;

namespace RovitNacional
{
    public enum Mode
    {
        TEA = 0,
        Down = 1,
        AC = 2
    }

    public enum Activity
    {
        BuldingTower = 0,
        GoStopGo = 1
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

    public static class Variables
    {
        public static string logOutput;
    }    
}