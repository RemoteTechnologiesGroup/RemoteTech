using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public delegate void OnClose();

    interface IGUIFragment {

        void Draw();

    }
}
