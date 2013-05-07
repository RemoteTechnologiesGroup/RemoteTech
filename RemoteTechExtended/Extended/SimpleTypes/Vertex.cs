using System.Collections.Generic;

namespace RemoteTech
{
    public class Vertex<T> : Vertex {
    
        public T Item;

        public Vertex(T item) {
            this.Item = item;
        }
    }

    abstract public class Vertex {

        public bool Visited { get; set; }

        List<Vertex> mEdges;

        public List<Vertex> Edges {
            get {
                return mEdges.AsReadOnly();
            }
        }

        public Vertex() {
            mEdges = new List<Vertex>();
        }
        
        public void AddEdge(Vertex edge) {
            mEdges.Add(edge);
        }
        
        public void RemoveEdge(Vertex item) {
            Edges.Remove(item);
        }


    }
}

