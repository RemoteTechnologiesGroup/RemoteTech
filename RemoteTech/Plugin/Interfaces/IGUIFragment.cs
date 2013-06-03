using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RemoteTech {
    public delegate void OnClick();
    public delegate void OnState(int state);
    public delegate void OnAntenna(IAntenna antenna);

    interface IGUIFragment {

        void Draw();

    }
}
