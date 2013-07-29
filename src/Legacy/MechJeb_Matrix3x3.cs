namespace RemoteTech.Legacy {
    public class Matrix3x3 {
        //row index, then column index
        private double[,] e = new double[3, 3];

        public double this[int i, int j] {
            get { return e[i, j]; }
            set { e[i, j] = value; }
        }

        public static Vector3d operator *(Matrix3x3 M, Vector3d v) {
            Vector3d ret = Vector3d.zero;
            for (int i = 0; i < 3; i++) {
                for (int j = 0; j < 3; j++) {
                    ret[i] += M.e[i, j] * v[j];
                }
            }
            return ret;
        }
    }
}

