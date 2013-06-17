namespace RemoteTech {
    public delegate void OnClick();

    public delegate void OnState(int state);

    public delegate void OnAntenna(IAntenna antenna);

    internal interface IFragment {
        void Draw();
    }
}
