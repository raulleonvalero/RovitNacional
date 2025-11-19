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
        private static string _nombre;
        private static int _nTurnos;

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

        public static string Nombre
        {
            get => _nombre;
            set => _nombre = value; 
        }

        public static int NTrunos
        {
            get => _nTurnos;
            set
            {
                if (value < 2) _nTurnos = 2;
                if (value > 14) _nTurnos = 14;
                else _nTurnos = value;
            }
        }
        
    }

    public static class Variables
    {
        public static bool experimentRunning = false;
        public static string logOutput;
    }    
}