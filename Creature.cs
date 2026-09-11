using System;
using System.Windows;

namespace DigitalMosquito
{
    public abstract class Creature
    {
        public abstract CreatureType CreatureType { get; }
        public abstract string Name { get; }

        public double X { get; protected set; }
        public double Y { get; protected set; }

        public double DisplayX { get; protected set; }
        public double DisplayY { get; protected set; }

        public double AngleDegrees { get; protected set; }

        public abstract double HitRadius { get; }
        public abstract FrameworkElement VisualElement { get; }

        private double _speedMultiplier = 1.0;
        public virtual double SpeedMultiplier
        {
            get => _speedMultiplier;
            set => _speedMultiplier = Math.Clamp(value, 0.2, 5.0);
        }

        public abstract BloodConfig BloodConfig { get; }

        public virtual int MaxHealth => 1;
        public virtual int CurrentHealth { get; set; } = 1;
        public virtual bool IsBoss => false;
        public virtual int XpReward => 10;

        public virtual bool TakeDamage(int damage)
        {
            CurrentHealth -= Math.Max(1, damage);
            return CurrentHealth <= 0;
        }

        public virtual void ResetHealth()
        {
            CurrentHealth = MaxHealth;
        }

        public abstract void ResetToRandomPosition(double screenWidth, double screenHeight);
        public abstract void Update(double deltaTime, double screenWidth, double screenHeight);

        protected readonly Point[] _positionHistory = new Point[8];
        protected int _historyCount = 0;
        protected int _historyIndex = 0;

        protected void RecordPosition(double x, double y)
        {
            _positionHistory[_historyIndex] = new Point(x, y);
            _historyIndex = (_historyIndex + 1) % _positionHistory.Length;
            if (_historyCount < _positionHistory.Length) _historyCount++;
        }

        public virtual bool IsHit(Point point)
        {
            // 1. Direct hit on current position
            double dx = point.X - DisplayX;
            double dy = point.Y - DisplayY;
            double radiusSq = HitRadius * HitRadius;
            if ((dx * dx + dy * dy) <= radiusSq) return true;

            // 2. Trail tolerance: check recent positions (past ~150ms) so fast-moving creatures can be swatted reliably
            for (int i = 0; i < _historyCount; i++)
            {
                var pt = _positionHistory[i];
                double hdx = point.X - pt.X;
                double hdy = point.Y - pt.Y;
                if ((hdx * hdx + hdy * hdy) <= radiusSq) return true;
            }

            return false;
        }

        public virtual void SetVisibility(bool visible)
        {
            VisualElement.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        // Web thread coordination for creatures that utilize webs (spiders)
        public virtual bool HasWebThread => false;
        public virtual Point WebStartPoint => new Point(DisplayX, 0);
        public virtual Point WebEndPoint => new Point(DisplayX, DisplayY);
    }
}
