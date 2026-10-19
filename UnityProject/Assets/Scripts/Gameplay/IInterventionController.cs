using System;

namespace UltimaJugada
{
    public interface IInterventionController : IDisposable
    {
        bool Finished { get; }
        void Tick(float deltaTime);
    }
}
