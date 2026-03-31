using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Numerics;

namespace Phoenix.AssetTool.Gui
{
    public class InputManager
    {
        private IInputContext _input;
        private IKeyboard _keyboard = default!;
        private IMouse _mouse;
        private Vector2 _lastMousePosition;

        public float MouseSensitivity = .001f;
        public Vector2 MouseDelta = Vector2.Zero;
        public int MouseWheelValue = 0;
        IWindow _window;
        public InputManager(IWindow window)
        {
            _window = window;
            _input = _window.CreateInput();

            //TODO: handle all mice/keyboard inputs
            _keyboard = _input.Keyboards.FirstOrDefault()!;
            //for (int i = 0; i < input.Mice.Count; i++)
            //{
            //    input.Mice[i].Cursor.CursorMode = CursorMode.Raw;
            //    input.Mice[i].MouseMove += OnMouseMove;
            //    input.Mice[i].Scroll += OnMouseWheel;
            //}


            _mouse = _input.Mice.FirstOrDefault()!;
            _mouse.Scroll += OnMouseWheel;
            _mouse.Cursor.CursorMode = CursorMode.Normal;
            _mouse.Position = (Vector2)_window.Position + (Vector2)_window.Size / 2;

            _lastMousePosition = _mouse.Position;
            MouseDelta = Vector2.Zero;

        }

        public IInputContext GetInputContext() { return _input; }

        public void SetMouseMode(CursorMode mode)
        {
            for (int i = 0; i < _input.Mice.Count; i++)
            {
                _input.Mice[i].Position = (Vector2)_window.Size / 2;
                _input.Mice[i].Cursor.CursorMode = mode;

            }
        }
        public void ToggleMouseMode()
        {
            var m = _mouse.Cursor.CursorMode == CursorMode.Normal ? CursorMode.Raw : CursorMode.Normal;
            //Console.WriteLine($"Mouse mode: {m}");
            SetMouseMode(m);
        }

        public void OnMouseWheel(IMouse mouse, ScrollWheel scrollWheel)
        {
            MouseWheelValue -= (int)scrollWheel.Y;
        }

        static List<Key> keysDown = new List<Key>();
        public void Update()
        {
            keysDown.RemoveAll(k => !_keyboard.IsKeyPressed(k));

            MouseDelta.X = (float)(_mouse.Position.X - _lastMousePosition.X) * MouseSensitivity;
            MouseDelta.Y = (float)(_mouse.Position.Y - _lastMousePosition.Y) * MouseSensitivity;
            _lastMousePosition = _mouse.Position;

        }
        public bool KeyDown(Key key)
        {
            return _keyboard.IsKeyPressed(key);
        }

        public bool KeyDownOnce(Key key)
        {
            if (_keyboard.IsKeyPressed(key) && !keysDown.Contains(key))
            {
                keysDown.Add(key);
                return true;
            }
            return false;
        }
    }
}
