using App.Common.Initialize;

namespace App.Main.Controller
{
    public class ControlDataReceptor : IInitializable
    {
        private Common.Controller.Controller controller;
        public int InitializationPriority => 0;
        public System.Type[] Dependencies => new System.Type[] { typeof(Common.Controller.Controller) };
        public void Initialize(ReferenceHolder referenceHolder)
        {
            controller = referenceHolder.GetInitializable<Common.Controller.Controller>();
        }

    }
}