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
        private static Activity _actividad = Activity.BuldingTower;
        private static Mode _modo = Mode.TEA;
        private static string _nombre = "Participante";
        private static int _nTurnos = 5;
        private static float _scale = 1.2f;
        private static float _timeLimit = 10.0f;

        public static string getActivityName()
        {
            switch (_actividad)
            {
                case Activity.BuldingTower:
                    return "Construir Torre";
                case Activity.GoStopGo:
                    return "Go Stop Go";
                default:
                    return "Actividad Desconocida";
            }
        }

        public static string getModeName()
        {
            switch (_modo)
            {
                case Mode.TEA:
                    return "TEA";
                case Mode.Down:
                    return "Down";
                case Mode.AC:
                    return "AC";
                default:
                    return "Modo Desconocido";
            }
        }

        public static Activity Actividad
        {
            get => _actividad;
            set => _actividad = value;
        }

        public static Mode Modo
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

        public static float Scale
        {
            get => _scale;
            set 
            {
                if (value < 1.0f) _scale = 1.0f;
                if (value > 2.0f) _scale = 2.0f;
                else _scale = value;
            }
        }

        public static float TimeLimit
        {
            get => _timeLimit;
            set 
            {
                if (value < 5.0f) _timeLimit = 5.0f;
                if (value > 30.0f) _timeLimit = 30.0f;
                else _timeLimit = value;
            }
        }
    }

    public static class Variables
    {
        public static bool experimentRunning = false;
        public static string logOutput;
    }    
}