using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinUiTemplate.Core.MVVM.Models
{
    public class GradientStep
    {
        // Fields
        private string _colourHex;
        private double _posiionX;
        private double _positionY;
        private double _offset;

        // Properties
        public string Colour {
            get => _colourHex;
            set {
                if (_colourHex == value) return;
                _colourHex = value;

                Updated?.Invoke(nameof(Colour));
            }
        }

        public double PositionX {
            get => _posiionX;
            set {
                value = Math.Clamp(value, 0, 1);
                if (_posiionX == value) return;
                _posiionX = value;

                Updated?.Invoke(nameof(PositionX));
            }
        }

        public double PositionY {
            get => _positionY;
            set {
                value = Math.Clamp(value, 0, 1);
                if (_positionY == value) return;
                _positionY = value;

                Updated?.Invoke(nameof(PositionY));
            }
        }

        public double Offset {
            get => _offset;
            set {
                value = Math.Clamp(value, 0, 1);
                if (_offset == value) return;
                _offset = value;

                Updated?.Invoke(nameof(Offset));
            }
        }

        // Events

        public event Action<string>? Updated;

        // Constructors

        public GradientStep() {
            _colourHex = "#232323";
            _posiionX = 0;
            _positionY = 0;
            _offset = 0;
        }

        public GradientStep(string colourHex, double positionX, double positionY, double offset) {
            _colourHex = colourHex;
            _posiionX = Math.Clamp(positionX, 0, 1);
            _positionY = Math.Clamp(positionY, 0, 1);
            _offset = Math.Clamp(offset, 0, 1);
        }

        public GradientStep(GradientStep other) {
            _colourHex = other.Colour;
            _posiionX = other.PositionX;
            _positionY = other.PositionY;
            _offset = other.Offset;
        }

        // Public Functions

        public void SetColourSilently(string colour) {
            _colourHex = colour;
        }
    }
}
