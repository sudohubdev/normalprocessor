using UnityEngine;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace dev.sudohub.normalprocessor
{
    public class GridState : INotifyPropertyChanged
    {
        //private fields
        private float _elementSize = 100f;
        private TextureType _textureType = TextureType.Input;

        //properties
        public float ElementSize
        {
            get => _elementSize;
            set
            {
                _elementSize = value;
                OnPropertyChanged();
            }
        }

        public TextureType TextureType
        {
            get => _textureType;
            set
            {
                _textureType = value;
                OnPropertyChanged();
            }
        }

        public Texture GetTexture(NormalProcessorGPU processor) => _textureType switch
        {
            TextureType.Input => processor.InputTexture,
            TextureType.Gauss => processor.TempTexture,
            _ => processor.OutputTexture
        };

        //format painter
        private FormatPainterState formatPainterState = FormatPainterState.Inactive;
        private readonly List<Vector2Int> _formatPainterConsumed = new();
        public void RequestFormatPainter() => formatPainterState = FormatPainterState.Requested;
        public bool ActivateFormatPainter()
        {
            if (formatPainterState == FormatPainterState.Requested)
            {
                formatPainterState = FormatPainterState.Active;
                return true;
            }
            return false;
        }
        public bool FormatPaintActive(Vector2Int coord)
        {
            if (formatPainterState != FormatPainterState.Active) return false;
            if (!_formatPainterConsumed.Contains(coord))
            {
                _formatPainterConsumed.Add(coord);
                return true;
            }
            return false;
        }
        public void StopFormatPaint()
        {
            if (formatPainterState == FormatPainterState.Inactive) return;
            formatPainterState = FormatPainterState.Inactive;
            _formatPainterConsumed.Clear();
        }

        //events
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            Debug.Log("GridState PropertyChanged <-" + (name ?? ""));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        internal void ClearEvents()
        {
            PropertyChanged = null;
        }
    }
}
