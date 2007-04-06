using System;

namespace EventSystem
{
    [Obsolete]
    internal interface IGameState
    {
        string Name { get; }
        void Enter();
        void Exit();
        void Pause();
        void Resume();
        bool KeyPressed(char key);
        bool FrameStarted();
    }
}