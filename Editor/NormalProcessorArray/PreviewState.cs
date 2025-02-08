using UnityEngine;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace dev.sudohub.normalprocessor
{
    public class PreviewState : INotifyPropertyChanged
    {
        //private fields
        private TextureType _textureType = TextureType.Normal;
        private FilterMode _filterMode = FilterMode.Point;
        private BackgroudType _background = BackgroudType.Black;

        //properties with events
        public TextureType TextureType
        {
            get => _textureType;
            set
            {
                _textureType = value;
                OnPropertyChanged();
            }
        }
        public FilterMode FilterMode
        {
            get => _filterMode;
            set
            {
                _filterMode = value;
                OnPropertyChanged();
            }
        }
        public BackgroudType Background
        {
            get => _background;
            set
            {
                _background = value;
                OnPropertyChanged();
            }
        }
        public Texture2D BackgroundTexture => Background switch
        {
            BackgroudType.Black => Texture2D.blackTexture,
            BackgroudType.Gray => Texture2D.grayTexture,
            _ => Texture2D.whiteTexture
        };
        public Texture GetTexture(NormalProcessorGPU processor) => _textureType switch
        {
            TextureType.Input or TextureType.InputFull => processor.InputTexture,
            TextureType.Gauss or TextureType.GaussFull => processor.TempTexture,
            _ => processor.OutputTexture
        };

        public Rect GetPrevievUVRect(NormalProcessorArrayState state)
        {
            if (_textureType is TextureType.InputFull or TextureType.GaussFull or TextureType.NormalFull)
            {
                //return full UV rect
                return new Rect(0, 0, 1, 1);
            }
            else
            {
                //partial UV rect
                return new Rect(
                    state.ActiveCoord.x / (float)state.Size.x,
                    1.0f - (state.ActiveCoord.y + 1) / (float)state.Size.y,
                    1.0f / (float)state.Size.x,
                    1.0f / (float)state.Size.y
                );
            }
        }

        //events
        public event PropertyChangedEventHandler PropertyChanged;
        //update preview
        internal void OnPropertyChanged([CallerMemberName] string name = null)
        {
            //Debug.Log("[Normal Processor] PreviewState PropertyChanged <-" + (name ?? ""));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
