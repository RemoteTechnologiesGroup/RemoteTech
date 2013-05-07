using System;

namespace RemoteTechExtended
{
    public static class PathfindingSolver {

        public class Node<T> {
            public delegate int CostFunctionDelegate(T a,T b);
            public delegate int HeuristicFunctionDelegate(T a,T b);
            T mItem;
            int mCost;
            int mHeuristic;
            CostFunctionDelegate mCostFunction;
            HeuristicFunctionDelegate mHeuristicFunction;
            
            public Node(T item, CostFunctionDelegate costFunction, HeuristicFunctionDelegate heuristicFunction) {
                this.mItem = item;
                this.mCostFunction = costFunction;
                this.mHeuristicFunction = heuristicFunction;
            }
        }
    }
}

