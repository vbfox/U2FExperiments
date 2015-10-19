namespace BlackFox.U2F.Gnubby
{
    public struct KeyRequest<TRequest>
    {
        public TRequest Request { get; }
        public InteractionFlags Flags { get; }

        public KeyRequest(TRequest request, InteractionFlags flags)
        {
            Request = request;
            Flags = flags;
        }
    }
}