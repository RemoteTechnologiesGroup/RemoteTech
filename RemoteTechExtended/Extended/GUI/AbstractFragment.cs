namespace RemoteTechExtended
{
    public abstract class AbtractFragment {

        // Properties
        protected RemoteCommandModule Module { 
            get { return mAttachedCommandModule; }
        }

        // Fields
        RemoteCommandModule mAttachedCommandModule;

        public AbtractFragment(RemoteCommandModule attachedTo) {
            this.mAttachedCommandModule = attachedTo;
        }

    }
}

