using System;

namespace AW2.Core.GameComponents
{
    public class PostFrameLogicEngine : AWGameComponent
    {
        public event Action DoOnce;
        public event Action DoEveryFrame;

        public PostFrameLogicEngine(AssaultWingCore game, int updateOrder)
            : base(game, updateOrder)
        {
        }

        public void Reset()
        {
            DoOnce = null;
            DoEveryFrame = null;
        }

        public override void Update()
        {
            if (DoOnce != null) DoOnce();
            DoOnce = null;
            if (DoEveryFrame != null) DoEveryFrame();
            base.Update();
        }
    }
}
