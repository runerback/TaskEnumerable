namespace Runerback.Utils
{
    public interface ITaskForEachController
    {
        /// <summary>
        /// break current travel
        /// </summary>
        void Break();
        /// <summary>
        /// skip next travel
        /// </summary>
        void Continue();
    }
}
