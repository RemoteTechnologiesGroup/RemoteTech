namespace RemoteTech
{
    public abstract class AbstractFragment {

        // Properties
        protected SPUPartModule Module { 
            get { return mAttachedCommandModule; }
        }

        // Fields
        SPUPartModule mAttachedCommandModule;

        public AbstractFragment(SPUPartModule attachedTo) {
            this.mAttachedCommandModule = attachedTo;
        }

    }
}

