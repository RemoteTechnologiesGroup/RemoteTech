using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    interface IGUIWindow {

        void Show();
        void Hide();

    }

    public abstract class AbstractGUIWindow : IGUIWindow {

        protected abstract void Draw();

        public virtual void Show() {
            RenderingManager.AddToPostDrawQueue(0, Draw);
        }

        public virtual void Hide() {
            RenderingManager.RemoveFromPostDrawQueue(0, Draw);
        }
    }
}
