using System;
using System.Windows;

namespace DigitalMosquito
{
    public enum GamePhase
    {
        Flying,
        DeadSplatter,
        Cleaning,
        Respawning
    }

    public class GameState
    {
        private GamePhase _currentPhase = GamePhase.Flying;
        private int _killCount = 0;

        public event Action<GamePhase>? PhaseChanged;
        public event Action<Point>? MosquitoKilled;
        public event Action? BloodCleaned;
        public event Action<int>? KillCountChanged;

        public GamePhase CurrentPhase
        {
            get => _currentPhase;
            private set
            {
                if (_currentPhase != value)
                {
                    _currentPhase = value;
                    PhaseChanged?.Invoke(_currentPhase);
                }
            }
        }

        public int KillCount
        {
            get => _killCount;
            private set
            {
                _killCount = value;
                KillCountChanged?.Invoke(_killCount);
            }
        }

        public void Kill(Point hitLocation)
        {
            if (CurrentPhase != GamePhase.Flying)
                return;

            KillCount++;
            CurrentPhase = GamePhase.DeadSplatter;
            MosquitoKilled?.Invoke(hitLocation);
        }

        public void StartCleaning()
        {
            if (CurrentPhase == GamePhase.DeadSplatter)
            {
                CurrentPhase = GamePhase.Cleaning;
            }
        }

        public void CompleteCleaning()
        {
            if (CurrentPhase == GamePhase.Cleaning || CurrentPhase == GamePhase.DeadSplatter)
            {
                CurrentPhase = GamePhase.Respawning;
                BloodCleaned?.Invoke();
            }
        }

        public void Respawn()
        {
            CurrentPhase = GamePhase.Flying;
        }
    }
}
