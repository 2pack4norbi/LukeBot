using System.Collections.Generic;
using System.Threading;
using LukeBot.Common;
using System;


namespace LukeBot.Config
{
    internal class PropertyDomain
    {
        public string mName { get; private set; }
        private Dictionary<string, Property> mProperties;
        private Mutex mMutex;

        public PropertyDomain(string name)
        {
            mName = name;
            mProperties = new Dictionary<string, Property>();
            mMutex = new Mutex();
        }

        ~PropertyDomain()
        {
            mName = null;
            mProperties.Clear();
            mProperties = null;
            mMutex = null;
        }

        // throws error if property already exists
        public void Add(Queue<string> path, Property p)
        {
            string name = path.Dequeue();

            if (path.Count == 0)
            {
                // end of path, our prop should be added to this domain
                p.SetName(name);
                if (!mProperties.TryAdd(name, p))
                {
                    throw new PropertyAlreadyExistsException("Failed to add property");
                }

                // added successfully
                return;
            }

            // not end of path, check if we have a domain like that
            Property prop;
            if (!mProperties.TryGetValue(name, out prop))
            {
                prop = Property.Create<PropertyDomain>(new PropertyDomain(name));
                prop.SetName(name);
                mProperties.Add(name, prop);
            }

            if (!prop.IsType(typeof(PropertyDomain)))
            {
                throw new PropertyNotADomainException("Expected property {0} to be a Domain", name);
            }

            PropertyDomain domain = prop.Get<PropertyDomain>();
            domain.Add(path, p);
        }

        public bool Exists(Queue<string> path)
        {
            string name = path.Dequeue();

            Property p;

            if (!mProperties.TryGetValue(name, out p))
            {
                return false;
            }

            if (path.Count == 0)
            {
                return true;
            }

            if (p.IsType(typeof(PropertyDomain)))
            {
                return p.Get<PropertyDomain>().Exists(path);
            }

            return false;
        }

        public bool Exists<T>(Queue<string> path)
        {
            string name = path.Dequeue();

            Property p;

            if (!mProperties.TryGetValue(name, out p))
            {
                return false;
            }

            if (path.Count == 0)
            {
                return p.IsType(typeof(T));
            }

            if (p.IsType(typeof(PropertyDomain)))
            {
                return p.Get<PropertyDomain>().Exists<T>(path);
            }

            return false;
        }

        public Property Get(Queue<string> path)
        {
            string name = path.Dequeue();

            Property p;

            if (!mProperties.TryGetValue(name, out p))
            {
                throw new PropertyNotFoundException("Property \"{0}\" not found", name);
            }

            if (path.Count == 0)
            {
                if (p.IsType(typeof(PropertyDomain)))
                {
                    throw new PropertyNotFoundException("Requested property {0} is a domain", name);
                }

                if (p.IsType(typeof(LazyProperty)))
                {
                    // this was loaded earlier but we couldn't create a Property out of it, possibly
                    // because required Assembly was not yet loaded. Retry creating the property.

                    // Store hidden parameter to pass it over to a new property
                    bool hidden = p.Hidden;

                    p = Property.CreateFromLazyProperty(p.Get<LazyProperty>());
                    p.SetName(name);
                    p.SetHidden(hidden);
                    mProperties.Remove(name);
                    mProperties.Add(name, p);
                }

                return p;
            }

            if (p.IsType(typeof(PropertyDomain)))
            {
                return p.Get<PropertyDomain>().Get(path);
            }

            throw new PropertyNotFoundException("Property of name {0} is a domain, not a Property", name);
        }

        public void Remove(Queue<string> path)
        {
            string name = path.Dequeue();

            if (path.Count == 0)
            {
                if (!mProperties.Remove(name))
                {
                    throw new PropertyNotFoundException("Couldn't find property to remove: {0}", name);
                }

                return;
            }

            Property p;

            if (!mProperties.TryGetValue(name, out p))
            {
                throw new PropertyNotFoundException("Couldn't find property domain to access for removal: {0}", name);
            }

            if (!p.IsType(typeof(PropertyDomain)))
            {
                throw new PropertyNotADomainException("Property mid-path is not a domain: {0}", name);
            }

            p.Get<PropertyDomain>().Remove(path);
        }

        internal void Accept(PropertyStoreVisitor v)
        {
            v.VisitStart(this);

            foreach (KeyValuePair<string, Property> e in mProperties)
            {
                e.Value.Accept(v);
            }

            v.VisitEnd(this);
        }
    }
}
