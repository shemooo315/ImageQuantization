using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Linq;

///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    public struct RGBPixelD
    {
        public double red, green, blue;
    }


    public class VertexParent
    {
        public bool done { get; set; } //O(1)

        public double weightOfedges { get; set; } = double.MaxValue; //O(1)
        public int cUrrentVertix { get; set; }//O(1)
        public int Parentvertix { get; set; } = -1; //O(1)
    }


    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>

    public class ImageOperations
    {


        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>

        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }

            return Buffer;
        }

        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }

        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);

            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

            unsafe
            {
                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                int nWidth = 0;
                nWidth = Width * 3;
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (int i = 0; i < Height; i++)
                {
                    for (int j = 0; j < Width; j++)
                    {
                        p[2] = ImageMatrix[i, j].red;
                        p[1] = ImageMatrix[i, j].green;
                        p[0] = ImageMatrix[i, j].blue;
                        p += 3;
                    }

                    p += nOffset;
                }
                ImageBMP.UnlockBits(bmd);
            }
            PicBox.Image = ImageBMP;
        }


        /// <summary>
        /// Apply Gaussian smoothing filter to enhance the edge detection 
        /// </summary>
        /// <param name="ImageMatrix">Colored image matrix</param>
        /// <param name="filterSize">Gaussian mask size</param>
        /// <param name="sigma">Gaussian sigma</param>
        /// <returns>smoothed color image</returns>
        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
        {
            int Height = GetHeight(ImageMatrix);
            int Width = GetWidth(ImageMatrix);

            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


            // Create Filter in Spatial Domain:
            //=================================
            //make the filter ODD size
            if (filterSize % 2 == 0) filterSize++;

            double[] Filter = new double[filterSize];

            //Compute Filter in Spatial Domain :
            //==================================
            double Sum1 = 0;
            int HalfSize = filterSize / 2;
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
                Sum1 += Filter[y + HalfSize];
            }
            for (int y = -HalfSize; y <= HalfSize; y++)
            {
                Filter[y + HalfSize] /= Sum1;
            }

            //Filter Original Image Vertically:
            //=================================
            int ii, jj;
            RGBPixelD Sum;
            RGBPixel Item1;
            RGBPixelD Item2;

            for (int j = 0; j < Width; j++)
                for (int i = 0; i < Height; i++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int y = -HalfSize; y <= HalfSize; y++)
                    {
                        ii = i + y;
                        if (ii >= 0 && ii < Height)
                        {
                            Item1 = ImageMatrix[ii, j];
                            Sum.red += Filter[y + HalfSize] * Item1.red;
                            Sum.green += Filter[y + HalfSize] * Item1.green;
                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
                        }
                    }
                    VerFiltered[i, j] = Sum;
                }

            //Filter Resulting Image Horizontally:
            //===================================
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                {
                    Sum.red = 0;
                    Sum.green = 0;
                    Sum.blue = 0;
                    for (int x = -HalfSize; x <= HalfSize; x++)
                    {
                        jj = j + x;
                        if (jj >= 0 && jj < Width)
                        {
                            Item2 = VerFiltered[i, jj];
                            Sum.red += Filter[x + HalfSize] * Item2.red;
                            Sum.green += Filter[x + HalfSize] * Item2.green;
                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
                        }
                    }
                    Filtered[i, j].red = (byte)Sum.red;
                    Filtered[i, j].green = (byte)Sum.green;
                    Filtered[i, j].blue = (byte)Sum.blue;
                }

            return Filtered;
        }


        public static List<RGBPixel> ListOfDC;
        public static List<int> listOfIndex;
        public static int FindDistinctColorsANDList(RGBPixel[,] ImageMatrix)
        {
            listOfIndex = new List<int>();
            ListOfDC = new List<RGBPixel>();
            //function O(N^2) , N->hight*width 
            int R, G, B;
            HashSet<int> Set = new HashSet<int>();
            HashSet<RGBPixel> Setij = new HashSet<RGBPixel>();
            for (int j = 0; j < GetHeight(ImageMatrix); j++)  //O(N)
            {
                for (int i = 0; i < GetWidth(ImageMatrix); i++)  //O(N)
                {
                    R = ImageMatrix[j, i].red;    //O(1)
                    G = ImageMatrix[j, i].green;  //O(1)
                    B = ImageMatrix[j, i].blue;   //O(1)
                    Set.Add(R + (G << 8) + (B << 16));  //O(1)
                    Setij.Add(ImageMatrix[j, i]); //O(1)
                }
            }
            listOfIndex = Set.ToList();  //O(1) num of distinct colors
            ListOfDC = Setij.ToList();   //O(1)
            return listOfIndex.Count();  //O(1)
        }

        public static float CalculateElcideanDistance(RGBPixel V1, RGBPixel V2)
        {
            //function O(1) distance between two vertex
            byte r1, r2, g1, g2, b1, b2;

            r1 = V1.red;
            g1 = V1.green;
            b1 = V1.blue;
            r2 = V2.red;
            g2 = V2.green;
            b2 = V2.blue;
            return (float)Math.Sqrt((r2 - r1) * (r2 - r1) + (g2 - g1) * (g2 - g1) + (b2 - b1) * (b2 - b1)); //O(1)
        }
        public static VertexParent[] vertixOf;//O(1)
        public static double MinimumSpanning()//O(E Log(V))
        {

            List<int> adjTosaveTree = new List<int>();//O(1)   
            Priorityqueue<VertexParent> queueNodeOf = new Priorityqueue<VertexParent>();//O(1)

            vertixOf = new VertexParent[ListOfDC.Count];//O(1)
            vertixOf[0] = new VertexParent() { weightOfedges = 0, Parentvertix = -1, cUrrentVertix = 0, done = false };
            queueNodeOf.Enqueue(vertixOf[0], vertixOf[0].weightOfedges);
            for (int i = 1; i < ListOfDC.Count; i++)//O(D)
            {
                vertixOf[i] = new VertexParent() { weightOfedges = CalculateElcideanDistance(ListOfDC[0], ListOfDC[i]), Parentvertix = -1, cUrrentVertix = i, done = false }; //O(1)
                queueNodeOf.Enqueue(vertixOf[i], vertixOf[i].weightOfedges);
            }
            double cost = 0.0;
            while (queueNodeOf.Count > 0)//O(N^2) whic N is size of queue
            {
                VertexParent getminver = queueNodeOf.Dequeue();   //O(1) the order of dequeue// to get the minimum priority of vertices
                int u = getminver.cUrrentVertix;//O(1)
                vertixOf[u].done = true;//O(1)
                cost += getminver.weightOfedges; //O(1)
                adjTosaveTree.Add(listOfIndex[getminver.cUrrentVertix]);
                for (int iterator = 0; iterator < ListOfDC.Count; iterator++)//O(D)
                {
                    if (!vertixOf[iterator].done) //O(1)
                    {
                        if (CalculateElcideanDistance(ListOfDC[u], ListOfDC[iterator]) < vertixOf[iterator].weightOfedges && CalculateElcideanDistance(ListOfDC[u], ListOfDC[iterator]) > 0)
                        {
                            vertixOf[iterator].Parentvertix = u; //O(1)
                            vertixOf[iterator].weightOfedges = CalculateElcideanDistance(ListOfDC[u], ListOfDC[iterator]);  //O(1)
                            queueNodeOf.UpdatePriority(vertixOf[iterator], vertixOf[iterator].weightOfedges); //O(
                        }
                    }
                }
            }
            return cost;
        }

        public struct Nodes_of_colores
        {
            public int nodeindex1 { get; set; }
            public int nodeindex2 { get; set; }
            public double Distance { get; set; }
            public int connectednodeindex1 { get; set; }
            public int connectednodeindex2 { get; set; }

        }
        public struct colorindecesmatrix
        {
            public int nodeindex1 { get; set; }
            public int nodeindex2 { get; set; }


        }
        public struct Nodes
        {
            public int node { get; set; }
            public int connectnode { get; set; }
            public float Distance { get; set; }

        }
        public struct Nodes_clusters
        {
            public int node { get; set; }

            public double Distance { get; set; }

        }
        public static float[] Distance;
        public static int[] pointvertices;
        public static List<KeyValuePair<int, int>> DistictColorsPixels_indeces;

        public static List<int>[] DistictColorsPixels;
        public static Dictionary<int, List<int>> Adjasent_list = new Dictionary<int, List<int>>();
        //function 3
        public static List<HashSet<int>> FindTheClustersForDistictColor(List<int> list_Msp, int numberofclusters)//O(K*D)
        {

            // int counter_forcalcmindistance = 0;
            List<HashSet<int>> ClustersofColors = new List<HashSet<int>>();//O(1)
            List<double> ResultOfEquation_Distance = new List<double>();//O(1)

            // Fill The adgacent List
            Editmsp(list_Msp, numberofclusters);//O(K*D)
            foreach (var item in list_Msp)//O(D) D are distict color
            {


                if (!Adjasent_list.ContainsKey(item))//O(1)
                    Adjasent_list.Add(item, new List<int>());//O(1)

                if (pointvertices[item] != -1 && !Adjasent_list.ContainsKey(pointvertices[item]))//O(1)
                    Adjasent_list.Add(pointvertices[item], new List<int>());//O(1)

                if (pointvertices[item] != -1)//O(1)
                {
                    Adjasent_list[pointvertices[item]].Add(item);//O(1)
                    Adjasent_list[item].Add(pointvertices[item]);//O(1)
                }
            }

            HashSet<int> visitedNodes = new HashSet<int>();//O(1)
            foreach (var vertex in Adjasent_list)//O(v)v are number vertices in each list
            {
                if (!visitedNodes.Contains(vertex.Key))// O(v)
                {
                    HashSet<int> set = new HashSet<int>();//O(1)
                    Removing_repeats(ref visitedNodes, vertex.Key, ref set);
                    ClustersofColors.Add(set);//O(1)

                }
            }
            return ClustersofColors;//O(1)

        }

        public static void Editmsp(List<int> list_Msp, int number_of_clusters) //O(K*D)
        {
            int loopcount = 0; //O(1)
            int numditictcolor = list_Msp.Count; //O(1)
            pointvertices = new int[numditictcolor]; //O(1)
            Distance = new float[numditictcolor]; //O(1)
            while (loopcount < number_of_clusters - 1)
            { //O(K)
                int c = 0; //O(1)
                int maxind = 0; //O(1)
                float max_distance = 0; //O(1)

                foreach (var item in list_Msp)
                {//O(D)



                    if (Distance[item] > max_distance)//O(1)
                    {
                        max_distance = Distance[item];//O(1)
                        maxind = item;//O(1)
                    }
                    c++;//O(1)
                }
                pointvertices[maxind] = -1;//O(1)
                Distance[maxind] = 0;//O(1)
                loopcount++;//O(1)
            }

        }
        public static void Removing_repeats(ref HashSet<int> visited, int currentvertex, ref HashSet<int> cluster_try)
        {


            cluster_try.Add(currentvertex);//O(1)
            visited.Add(currentvertex);//O(1)
            List<int> list_tring = Adjasent_list[currentvertex];//O(1)
            foreach (var neighbour in list_tring)//O(v) v are vertices in each list in list_tring
            {
                if (!visited.Contains(neighbour))//O(v)
                    Removing_repeats(ref visited, neighbour, ref cluster_try);
            }





        }


        // function 4
        public static Dictionary<int, int> FindTherepresentiveColorForeachcluster(List<HashSet<int>> ClustersofColors, int number_distenctcolor)//O(D)=O(K*N) D is ditinct color For every Cluster
        {
            int avrred = 0, avrgreen = 0, avrblue = 0;//O(1)
            int counterrr;//O(1)
            int finalcolor;//O(1)
            int[] collection_colorredsum = new int[number_distenctcolor];//O(1)
            int[] collection_colorgreensum = new int[number_distenctcolor];//O(1)
            int[] collection_colorbluesum = new int[number_distenctcolor];//O(1) 
            Dictionary<int, int> colorandreprsentivecolor = new Dictionary<int, int>();//O(1)
            foreach (var item1 in ClustersofColors)
            {//O(K)
                HashSet<int> hass = item1;//O(1)
                counterrr = 0;//O(1)
                avrred = 0;//O(1)
                avrgreen = 0;//O(1)
                avrblue = 0;//O(1)
                finalcolor = 0;//O(1)
                foreach (var item2 in hass)
                {//O(n) n is ditinct color For every Cluster
                    collection_colorredsum[counterrr] = (byte)(item2 >> 16);//O(1)
                    collection_colorgreensum[counterrr] = (byte)(item2 >> 8);//O(1)
                    collection_colorbluesum[counterrr] = (byte)(item2);//O(1)
                    counterrr = counterrr + 1;//O(1)
                }
                for (int c = 0; c < counterrr; c++) //O(D) D is ditinct color For every Cluster
                {
                    avrred += collection_colorredsum[counterrr];//O(1) 
                    avrgreen += collection_colorgreensum[counterrr];//O(1) 
                    avrblue += collection_colorbluesum[counterrr];//O(1) 


                }
                avrred = avrred / counterrr;//O(1) 
                avrgreen = avrgreen / counterrr;//O(1) 
                avrblue = avrblue / counterrr;//O(1) 
                finalcolor = (avrred << 16) + (avrgreen << 8) + (avrblue);//O(1) 
                foreach (var item2 in hass)//O(D) D is ditinct color For every Cluster
                {
                    colorandreprsentivecolor.Add(item2, finalcolor);//O(1) 
                }

                Array.Clear(collection_colorredsum, 0, counterrr);//O(D) D is ditinct color For every Cluster
                Array.Clear(collection_colorgreensum, 0, counterrr);//O(D) D is ditinct color For every Cluster
                Array.Clear(collection_colorbluesum, 0, counterrr);//O(D) D is ditinct color For every Cluster
            }
            return colorandreprsentivecolor;//O(1) 


        }
        // function 5
        public static void QuantizationTheImage(RGBPixel[,] Matrixforimagepath, Dictionary<int, int> colorandreprsentivecolor)
        {
            int color = 0;//O(1) 
            int counter_rows = GetHeight(Matrixforimagepath);//O(1) 
            int counter_columns = GetWidth(Matrixforimagepath);//O(1) 
            Dictionary<int, colorindecesmatrix> listcolors = new Dictionary<int, colorindecesmatrix>(counter_rows * counter_columns);//O(1) 
            int counter_loop1 = 0;//O(1) 
            int counter_loop2 = 0;//O(1) 

            colorindecesmatrix struc = new colorindecesmatrix();//O(1) 
            while (counter_loop1 < counter_rows)
            {//O(N)

                while (counter_loop2 < counter_columns)//O(N)
                {
                    int red = Matrixforimagepath[counter_loop1, counter_loop2].red;//O(1)
                    int blue = Matrixforimagepath[counter_loop1, counter_loop2].blue;//O(1)
                    int green = Matrixforimagepath[counter_loop1, counter_loop2].green;//O(1)
                    color = (red << 16) + (green << 8) + blue;//O(1)
                    struc.nodeindex1 = counter_loop1;//O(1)
                    struc.nodeindex2 = counter_loop2;//O(1)
                    listcolors.Add(color, struc);//O(1)

                    counter_loop2 = counter_loop2 + 1;//O(1)
                }
                counter_loop1 = counter_loop1 + 1;//O(1)

            }
            foreach (var item in listcolors)//O(N^2)
            {
                int value = colorandreprsentivecolor[item.Key];//O(1)
                Matrixforimagepath[item.Value.nodeindex1, item.Value.nodeindex2].red = (byte)(value >> 16);//O(1)
                Matrixforimagepath[item.Value.nodeindex1, item.Value.nodeindex2].green = (byte)(value >> 8);//O(1)
                Matrixforimagepath[item.Value.nodeindex1, item.Value.nodeindex2].blue = (byte)(value);//O(1)

            }



        }



    }



}
public class Priorityqueue<T>
{
    public struct Node
    {
        public double Priority;  //O(1)
        public T Object { get; set; } //O(1)
    }
    List<Node> queue = new List<Node>(); //O(1)
    static int SizeOFHeap = -1; //O(1)

    public int Count { get { return queue.Count; } } //O(1)
                                                     //public int Count1 { get; } // O(1)
    public int CLeft(int i) //O(1)
    {
        return i * 2 + 1; //O(1)
    }
    public int CRight(int i) //O(1)
    {
        return i * 2 + 2; //O(1)
    }

    public void Exchange(int iiii, int jjj)
    {
        var temp = queue[iiii]; //O(1)
        queue[iiii] = queue[jjj]; //O(1)
        queue[jjj] = temp; //O(1)
    }
    public void Enqueue(T obj, double priority)
    {
        Node node = new Node() { Priority = priority, Object = obj };
        queue.Add(node);
        SizeOFHeap++; //O(1)

    }
    public T Dequeue() //O(1)
    {
        if (SizeOFHeap > -1) //O(1)
        {

            var returnVal = queue[0].Object; //O(1)
            queue[0] = queue[SizeOFHeap];//O(1)
            queue.RemoveAt(SizeOFHeap); //O(1) w
            SizeOFHeap--;//O(1)



            return returnVal;
        }
        else
            throw new Exception("Queue is empty");

    }

    public void UpdatePriority(T obj, double priority) //O(s)
    {


        for (int i = 0; i <= SizeOFHeap; i++) //O(s) sizeOfheap
        {
            Node node = queue[i];
            if (object.ReferenceEquals(node.Object, obj))
            {
                node.Priority = priority;//O(1)
                queue[i] = node;
                BuildHeapMin(i);
                MinimumHeapOfP(i);
            }
        }
    }
    public void MinimumHeapOfP(int i)
    {
        int l = CLeft(i); //O(1)
        int R = CRight(i); //O(1)
        int lowest = i; //O(1)

        if (l <= SizeOFHeap && queue[l].Priority > queue[lowest].Priority)
        {
            lowest = l; //O(1)
        }
        else
        {
            lowest = i; //O(1)
        }
        if (R <= SizeOFHeap && queue[R].Priority < queue[lowest].Priority)
        {
            i = R; //O(1)
        }
        if (lowest != i)
        {
            Exchange(lowest, i);
            MinimumHeapOfP(lowest);
        }
    }

    private void BuildHeapMin(int i)
    {
        int pObject = (i - 1) / 2;
        while (i >= 0 && queue[pObject].Priority < queue[i].Priority)
        {
            Exchange(i, pObject);
            i = pObject;
        }
    }

}






////    /* class VertexParent 
////     {
////         public VertexParent()
////         { }
////         public VertexParent(int vertex, int? parent)
////         {
////             V = vertex;
////             P = parent;
////         }
////         public int V { get; set; }       
////         public int? P { get; set; }        

////     }

////     public class Graph
////     {
////         public int V;
////         public List<int>[] adjacentListArray;
////         public int[] values;

////         public Graph(int V, int[] values)
////         {
////             this.V = V;
////             this.values = values;
////             adjacentListArray = new List<int>[V];

////             for (int i = 0; i < V; i++)
////             {
////                 adjacentListArray[i] = new List<int>();
////             }
////         }
////         public void addEdge(int src, int dest)
////         {
////             adjacentListArray[src - 1].Add(dest - 1);
////             adjacentListArray[dest - 1].Add(src - 1);
////         }
////     }
////     struct Edge
////     {
////         public int V1;      // first color
////         public int V2;      // secound color
////         public float Weight;  // distance between 2 color
////     }*/

////    /// <summary>
////    /// Library of static functions that deal with images
////    /// </summary>

////    public class ImageOperations
////    {

////        /// <summary>
////        /// Open an image and load it into 2D array of colors (size: Height x Width)
////        /// </summary>
////        /// <param name="ImagePath">Image file path</param>
////        /// <returns>2D array of colors</returns>
////        public static RGBPixel[,] OpenImage(string ImagePath)
////        {
////            Bitmap original_bm = new Bitmap(ImagePath);
////            int Height = original_bm.Height;
////            int Width = original_bm.Width;

////            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

////            unsafe
////            {
////                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
////                int x, y;
////                int nWidth = 0;
////                bool Format32 = false;
////                bool Format24 = false;
////                bool Format8 = false;

////                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
////                {
////                    Format24 = true;
////                    nWidth = Width * 3;
////                }
////                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
////                {
////                    Format32 = true;
////                    nWidth = Width * 4;
////                }
////                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
////                {
////                    Format8 = true;
////                    nWidth = Width;
////                }
////                int nOffset = bmd.Stride - nWidth;
////                byte* p = (byte*)bmd.Scan0;
////                for (y = 0; y < Height; y++)
////                {
////                    for (x = 0; x < Width; x++)
////                    {
////                        if (Format8)
////                        {
////                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
////                            p++;
////                        }
////                        else
////                        {
////                            Buffer[y, x].red = p[2];
////                            Buffer[y, x].green = p[1];
////                            Buffer[y, x].blue = p[0];
////                            if (Format24) p += 3;
////                            else if (Format32) p += 4;
////                        }
////                    }
////                    p += nOffset;
////                }
////                original_bm.UnlockBits(bmd);
////            }

////            return Buffer;
////        }

////        /// <summary>
////        /// Get the height of the image 
////        /// </summary>
////        /// <param name="ImageMatrix">2D array that contains the image</param>
////        /// <returns>Image Height</returns>
////        public static int GetHeight(RGBPixel[,] ImageMatrix)
////        {
////            return ImageMatrix.GetLength(0);
////        }

////        /// <summary>
////        /// Get the width of the image 
////        /// </summary>
////        /// <param name="ImageMatrix">2D array that contains the image</param>
////        /// <returns>Image Width</returns>
////        public static int GetWidth(RGBPixel[,] ImageMatrix)
////        {
////            return ImageMatrix.GetLength(1);
////        }

////        /// <summary>
////        /// Display the given image on the given PictureBox object
////        /// </summary>
////        /// <param name="ImageMatrix">2D array that contains the image</param>
////        /// <param name="PicBox">PictureBox object to display the image on it</param>
////        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox)
////        {
////            // Create Image:
////            //==============
////            int Height = ImageMatrix.GetLength(0);
////            int Width = ImageMatrix.GetLength(1);

////            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);

////            unsafe
////            {
////                BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
////                int nWidth = 0;
////                nWidth = Width * 3;
////                int nOffset = bmd.Stride - nWidth;
////                byte* p = (byte*)bmd.Scan0;
////                for (int i = 0; i < Height; i++)
////                {
////                    for (int j = 0; j < Width; j++)
////                    {
////                        p[2] = ImageMatrix[i, j].red;
////                        p[1] = ImageMatrix[i, j].green;
////                        p[0] = ImageMatrix[i, j].blue;
////                        p += 3;
////                    }

////                    p += nOffset;
////                }
////                ImageBMP.UnlockBits(bmd);
////            }
////            PicBox.Image = ImageBMP;
////        }


////        /// <summary>
////        /// Apply Gaussian smoothing filter to enhance the edge detection 
////        /// </summary>
////        /// <param name="ImageMatrix">Colored image matrix</param>
////        /// <param name="filterSize">Gaussian mask size</param>
////        /// <param name="sigma">Gaussian sigma</param>
////        /// <returns>smoothed color image</returns>
////        public static RGBPixel[,] GaussianFilter1D(RGBPixel[,] ImageMatrix, int filterSize, double sigma)
////        {
////            int Height = GetHeight(ImageMatrix);
////            int Width = GetWidth(ImageMatrix);

////            RGBPixelD[,] VerFiltered = new RGBPixelD[Height, Width];
////            RGBPixel[,] Filtered = new RGBPixel[Height, Width];


////            // Create Filter in Spatial Domain:
////            //=================================
////            //make the filter ODD size
////            if (filterSize % 2 == 0) filterSize++;

////            double[] Filter = new double[filterSize];

////            //Compute Filter in Spatial Domain :
////            //==================================
////            double Sum1 = 0;
////            int HalfSize = filterSize / 2;
////            for (int y = -HalfSize; y <= HalfSize; y++)
////            {
////                //Filter[y+HalfSize] = (1.0 / (Math.Sqrt(2 * 22.0/7.0) * Segma)) * Math.Exp(-(double)(y*y) / (double)(2 * Segma * Segma)) ;
////                Filter[y + HalfSize] = Math.Exp(-(double)(y * y) / (double)(2 * sigma * sigma));
////                Sum1 += Filter[y + HalfSize];
////            }
////            for (int y = -HalfSize; y <= HalfSize; y++)
////            {
////                Filter[y + HalfSize] /= Sum1;
////            }

////            //Filter Original Image Vertically:
////            //=================================
////            int ii, jj;
////            RGBPixelD Sum;
////            RGBPixel Item1;
////            RGBPixelD Item2;

////            for (int j = 0; j < Width; j++)
////                for (int i = 0; i < Height; i++)
////                {
////                    Sum.red = 0;
////                    Sum.green = 0;
////                    Sum.blue = 0;
////                    for (int y = -HalfSize; y <= HalfSize; y++)
////                    {
////                        ii = i + y;
////                        if (ii >= 0 && ii < Height)
////                        {
////                            Item1 = ImageMatrix[ii, j];
////                            Sum.red += Filter[y + HalfSize] * Item1.red;
////                            Sum.green += Filter[y + HalfSize] * Item1.green;
////                            Sum.blue += Filter[y + HalfSize] * Item1.blue;
////                        }
////                    }
////                    VerFiltered[i, j] = Sum;
////                }

////            //Filter Resulting Image Horizontally:
////            //===================================
////            for (int i = 0; i < Height; i++)
////                for (int j = 0; j < Width; j++)
////                {
////                    Sum.red = 0;
////                    Sum.green = 0;
////                    Sum.blue = 0;
////                    for (int x = -HalfSize; x <= HalfSize; x++)
////                    {
////                        jj = j + x;
////                        if (jj >= 0 && jj < Width)
////                        {
////                            Item2 = VerFiltered[i, jj];
////                            Sum.red += Filter[x + HalfSize] * Item2.red;
////                            Sum.green += Filter[x + HalfSize] * Item2.green;
////                            Sum.blue += Filter[x + HalfSize] * Item2.blue;
////                        }
////                    }
////                    Filtered[i, j].red = (byte)Sum.red;
////                    Filtered[i, j].green = (byte)Sum.green;
////                    Filtered[i, j].blue = (byte)Sum.blue;
////                }

////            return Filtered;
////        }

////        ////*********************************************************************************////
////        ///
////        //add using System.Linq;
////        // function 1
////        public static int NumOfDistinctColor(RGBPixel[,] ImageMatrix)
////        {
////            int R, G, B;
////            List<int> ListOfDC = new List<int>();

////            HashSet<int> Set = new HashSet<int>();
////            for (int j = 0; j < GetHeight(ImageMatrix); j++)
////            {
////                for (int i = 0; i < GetWidth(ImageMatrix); i++)
////                {
////                    R = ImageMatrix[j, i].red;
////                    G = ImageMatrix[j, i].green;
////                    B = ImageMatrix[j, i].blue;
////                    Set.Add(R + (G << 8) + (B << 16));
////                    Console.WriteLine(R);
////                    Console.WriteLine(G << 8);
////                    Console.WriteLine(B << 16);
////                    Console.WriteLine(R + (G << 8) + (B << 16));
////                }
////            }
////            ListOfDC = Set.ToList();
////            return ListOfDC.Count();
////        }
////        public static List<int> ListDistinctColor(RGBPixel[,] ImageMatrix)
////        {
////            int R, G, B;
////            List<int> ListOfDC = new List<int>();

////            HashSet<int> Set = new HashSet<int>();
////            for (int j = 0; j < GetHeight(ImageMatrix); j++)
////            {
////                for (int i = 0; i < GetWidth(ImageMatrix); i++)
////                {
////                    R = ImageMatrix[j, i].red;
////                    G = ImageMatrix[j, i].green;
////                    B = ImageMatrix[j, i].blue;
////                    Set.Add(R + (G << 8) + (B << 16));
////                }
////            }
////            ListOfDC = Set.ToList();
////            txtDiscolor.Text = ListOfDC.Count();
////            return ListOfDC;
////        }

////        ///////////////////////////////////////////////////////////////////////////
////        public struct Edges
////        {
////            public int fromVertix1;// { get; set; }
////            public int toVertix2;//{ get; set; }
////            public float w;// { get; set; }
////        }

////        public struct VertexParent 
////        {
////            public float Priority { get; set; }
////            public int V { get; set; }          // current vertix
////            public int? P { get; set; }  // parent vertix
////        }
////        // function 2
////        private static float CalculateDistances(VertexParent V1, VertexParent V2)
////        {
////            byte r1, r2, g1, g2, b1, b2;
////            r1 = (byte)(V1.V >> 16);
////            r2 = (byte)(V2.V >> 16);
////            g1 = (byte)(V1.V >> 8);
////            g2 = (byte)(V2.V >> 8);
////            b1 = (byte)(V1.V);
////            b2 = (byte)(V2.V);
////            return (float)Math.Sqrt(Math.Pow(r2 - r1, 2) + Math.Pow(g2 - g1, 2) + Math.Pow(b2 - b1, 2));
////        }
////        // function 3
////        public static List<Edges> MinimumSpanning(List<Edges> ListOfGraph)
////        {
////            List<Edges> adj = new List<Edges>();
////            // Priorityqueue<VertexParent>.Node priority = Priorityqueue<VertexParent> ();
////            VertexParent vert = new VertexParent { V = int.MaxValue, P = null };
////            Priorityqueue<VertexParent> queueN = new Priorityqueue<VertexParent>(ListOfGraph.Count);
////            VertexParent[] vertixOf = new VertexParent[ListOfGraph.Count];

////            vertixOf[0] = new VertexParent { V = ListOfGraph[0].fromVertix1, P = null, Priority = 0 }; // initializing the first node in the MST.

////            queueN.Enqueue(0, vertixOf[0]);
////            for (int i = 0; i < ListOfGraph.Count; i++)
////            {
////                vertixOf[i] = new VertexParent { V = ListOfGraph[i].fromVertix1, P = null, Priority = ListOfGraph[i].w };
////                queueN.Enqueue(int.MaxValue, vertixOf[i]);

////            }

////            int totalcost = 0;
////            //Priorityqueue<Edges>.Node pr = new Priorityqueue<Edges>.Node();
////            while (queueN.Count > 0)
////            {

////                VertexParent getminver = queueN.Dequeue();   // get the minimum priority
////                if (getminver.P != null)        // if it is not the starting node.
////                {

////                    Edges e;
////                    e.fromVertix1 = getminver.V;
////                    e.toVertix2 = (int)getminver.P;

////                    e.w = (float)getminver.Priority;
////                    adj.Add(e);    // add the minimum weight to the MST.
////                    totalcost++;

////                }
////                //float w;
////                //(IEnumerable)
////                /* foreach (var item in queueN){   // modify the priority each time .

////                      w = CalculateDistances(item, getminver);  // calculates the weight between the current node and the top node.
////                      if (w < item.Priority)
////                      {
////                          item.P = getminver.V;
////                          queueN.UpdatePriority(item, w);
////                      }
////                  }*/
////            }

////            return adj;
////        }

////        public class Priorityqueue<T>
////        {
////            public struct Node
////            {
////                public int Priority;
////                public T Object { get; set; }
////            }
////            List<Node> queue = new List<Node>();
////            static int SizeOFHeap = -1;
////            //int[] arr = new int[SizeOFHeap];
////            bool isMinQueue;

////            public Priorityqueue(bool _isMinQueue = false)
////            {
////                isMinQueue = _isMinQueue;

////            }
////            public Priorityqueue(int count)
////            {
////                Count1 = count;
////            }
////            public int Count { get { return queue.Count; } }
////            public int Count1 { get; }
////            public int CLeft(int i)
////            {
////                return i * 2 + 1;
////            }
////            public int CRight(int i)
////            {
////                return i * 2 + 2;
////            }
////            public void Exchange(int i, int j)
////            {
////                var temp = queue[i];
////                queue[i] = queue[j];
////                queue[j] = temp;
////            }
////            public void Enqueue(int priority, T obj)
////            {
////                Node node = new Node() { Priority = priority, Object = obj };
////                queue.Add(node);
////                SizeOFHeap++;
////                //Maintaining heap

////                if (!isMinQueue)
////                    BuildMaxHeap(SizeOFHeap);
////            }
////            public T Dequeue()
////            {
////                if (SizeOFHeap > -1)
////                {
////                    var returnVal = queue[0].Object;
////                    queue[0] = queue[SizeOFHeap];
////                    queue.RemoveAt(SizeOFHeap);
////                    SizeOFHeap--;
////                    //Maintaining lowest or highest at root based on min or max queue
////                    if (isMinQueue)
////                        MainiHeap(0);
////                    else
////                        MaxHeap(0);
////                    return returnVal;
////                }
////                else
////                    throw new Exception("Queue is empty");

////            }

////            public void UpdatePriority(T obj, int priority)
////            {

////                for (int i = 0; i <= SizeOFHeap; i++)
////                {
////                    /*Node node = queue[i];
////                    if (object.ReferenceEquals(node.Object, obj))
////                    {
////                        node.Priority = priority;
////                        if (isMinQueue)
////                        {
////                            BuildHeapMin(i);
////                            MainiHeap(i);
////                        }
////                        else
////                        {
////                            BuildMaxHeap(i);
////                            MaxHeap(i);
////                        }
////                    }*/
////                }
////            }
////            /*public bool Exist(T obj)
////            {
////                foreach (Node node in queue)
////                    if (object.ReferenceEquals(node.Object, obj))
////                        return true;
////                return false;
////            }*/
////            public void EXtractMax()
////            {
////                if (SizeOFHeap < 1)
////                {
////                    throw new Exception("heapunderflow");
////                }

////            }
////            public void MaxHeap(int i)
////            {
////                //check which element is largest and swap elements to put max element parent of elements under it
////                int l = CLeft(i);
////                int R = CRight(i);
////                int largest = i;

////                if (l <= SizeOFHeap && queue[l].Priority > queue[largest].Priority)
////                {
////                    largest = l;
////                }
////                else
////                {
////                    largest = i;
////                }
////                if (R <= SizeOFHeap && queue[R].Priority > queue[largest].Priority)
////                {
////                    i = R;
////                }
////                if (largest != i)
////                {
////                    Exchange(largest, i);
////                    MaxHeap(largest);
////                }
////            }
////            public void MainiHeap(int i)
////            {
////                int l = CLeft(i);
////                int R = CRight(i);
////                int lowest = i;

////                if (l <= SizeOFHeap && queue[l].Priority > queue[lowest].Priority)
////                {
////                    lowest = l;
////                }
////                else
////                {
////                    lowest = i;
////                }
////                if (R <= SizeOFHeap && queue[R].Priority > queue[lowest].Priority)
////                {
////                    i = R;
////                }
////                if (lowest != i)
////                {
////                    Exchange(lowest, i);
////                    MainiHeap(lowest);
////                }
////            }
////            public void BuildMaxHeap(int i)
////            {
////                for (i = queue[(i - 1) / 2].Priority; i > 0; i--)
////                {
////                    if (queue[(i - 1) / 2].Priority > queue[i].Priority)
////                    {
////                        Exchange(i, (i - 1) / 2);
////                        i = (i - 1) / 2;
////                    }
////                }
////                //Build max with exchange elements to put largest as parent
////                /* while (i >= 0&&queue[(i-1)/2].Priority>queue[i].Priority)
////                 {
////                     Exchange(i, (i - 1) / 2);
////                     i = (i - 1) / 2;
////                 }*/
////            }
////            private void BuildHeapMin(int i)
////            {
////                while (i >= 0 && queue[(i - 1) / 2].Priority > queue[i].Priority)
////                {
////                    Exchange(i, (i - 1) / 2);
////                    i = (i - 1) / 2;
////                }
////            }

////        }

////        ///////////////////////////////////////////////

////        public static List<KeyValuePair<int, int>> DistictColorsPixels_indeces;

////        public static List<int>[] DistictColorsPixels;
////        public static Dictionary<int, List<Nodes_clusters>> Adjasent_list = new Dictionary<int, List<Nodes_clusters>>();
////        public struct Nodes_of_colores
////        {
////            public int nodeindex1 { get; set; }
////            public int nodeindex2 { get; set; }
////            public double Distance { get; set; }
////            public int connectednodeindex1 { get; set; }
////            public int connectednodeindex2 { get; set; }

////        }
////        public struct colorindecesmatrix
////        {
////            public int nodeindex1 { get; set; }
////            public int nodeindex2 { get; set; }


////        }
////        public struct Nodes
////        {
////            public int node { get; set; }
////            public int connectnode { get; set; }
////            public float Distance { get; set; }

////        }
////        public struct Nodes_clusters
////        {
////            public int node { get; set; }

////            public double Distance { get; set; }

////        }
////        public static void Editmsp(List<Nodes> list_Msp, int number_of_clusters)
////        {
////            int loopcount = 0;
////            while (loopcount < number_of_clusters - 1)
////            {
////                int c = 0;
////                int maxind = 0;
////                double max_distance = 0;
////                Nodes n = new Nodes { node = 0, connectnode = 0, Distance = 0 };
////                foreach (var item in list_Msp)
////                {


////                    if (item.Distance > max_distance)
////                    {
////                        max_distance = item.Distance;
////                        n = item;
////                        n.Distance = 0;
////                        maxind = c;


////                    }
////                    c++;
////                }
////                list_Msp[maxind] = n;
////                loopcount++;
////            }

////        }
////        public static void Removing_repeats(ref HashSet<int> visited, int currentvertex, ref HashSet<int> cluster_try)
////        {


////            cluster_try.Add(currentvertex);
////            visited.Add(currentvertex);
////            List<Nodes_clusters> list_tring = Adjasent_list[currentvertex];
////            foreach (var neighbour in list_tring)
////            {
////                if (!visited.Contains(neighbour.node))
////                    Removing_repeats(ref visited, neighbour.node, ref cluster_try);
////            }

////        }
////        public static List<HashSet<int>> FindTheClustersForDistictColor(List<Nodes> list_Msp, int numberofclusters)
////        {

////            // int counter_forcalcmindistance = 0;
////            List<HashSet<int>> ClustersofColors = new List<HashSet<int>>();
////            List<double> ResultOfEquation_Distance = new List<double>();

////            // list menna msp          
////            List<Nodes> n = new List<Nodes>();
////            int numditictcolor = n.Count;
////            List<Nodes_clusters> Extention_adjlist = new List<Nodes_clusters>(numditictcolor);
////            /* foreach (var item in n) {

////                 Adjasent_list[item.node] = new List<Nodes_clusters>();
////             }
////            //Arrang the tree Ascending Tree
////            double Minimum_Distance = 1000;//float.MaxValue;
////            foreach (var item in n)
////            {

////                if (item.Distance< Minimum_Distance) {
////                    Minimum_Distance = item.Distance;



////                }
////                counter_forcalcmindistance = counter_forcalcmindistance + 1;

////            }*/
////            // Fill The adgacent List
////            Editmsp(list_Msp, numberofclusters);
////            foreach (var item in n)
////            {

////                if (item.Distance != 0)
////                {

////                    if (Adjasent_list.ContainsKey(item.node))
////                    {
////                        Nodes_clusters obj1 = new Nodes_clusters { node = item.connectnode, Distance = item.Distance };
////                        Adjasent_list[item.node].Add(obj1);
////                    }
////                    else
////                    {
////                        List<Nodes_clusters> list = new List<Nodes_clusters>();
////                        Nodes_clusters obj2 = new Nodes_clusters { node = item.connectnode, Distance = item.Distance };
////                        list.Add(obj2);
////                        Adjasent_list.Add(item.node, list);
////                    }
////                    if (Adjasent_list.ContainsKey(item.connectnode))
////                    {

////                        Nodes_clusters obj3 = new Nodes_clusters { node = item.node, Distance = item.Distance };
////                        Adjasent_list[item.connectnode].Add(obj3);
////                    }
////                    else
////                    {
////                        List<Nodes_clusters> list = new List<Nodes_clusters>();
////                        Nodes_clusters obj4 = new Nodes_clusters { node = item.node, Distance = item.Distance };
////                        list.Add(obj4);
////                        Adjasent_list.Add(item.connectnode, list);
////                    }

////                }
////                else
////                {
////                    if (!Adjasent_list.ContainsKey(item.node))
////                    {
////                        List<Nodes_clusters> list = new List<Nodes_clusters>();
////                        Adjasent_list.Add(item.node, list);
////                    }
////                    if (!Adjasent_list.ContainsKey(item.connectnode))
////                    {
////                        List<Nodes_clusters> list = new List<Nodes_clusters>();
////                        Adjasent_list.Add(item.connectnode, list);
////                    }




////                }
////            }

////            HashSet<int> visitedNodes = new HashSet<int>();
////            foreach (var vertex in Adjasent_list)
////            {
////                if (!visitedNodes.Contains(vertex.Key))
////                {
////                    HashSet<int> set = new HashSet<int>();
////                    Removing_repeats(ref visitedNodes, vertex.Key, ref set);
////                    ClustersofColors.Add(set);

////                }
////            }
////            return ClustersofColors;

////        }
////        // function 4
////        public static Dictionary<int, int> FindTherepresentiveColorForeachcluster(List<HashSet<int>> ClustersofColors, int number_distenctcolor)
////        {
////            int avrred = 0, avrgreen = 0, avrblue = 0;
////            int counterrr;
////            int finalcolor;
////            int[] collection_colorredsum = new int[number_distenctcolor];
////            int[] collection_colorgreensum = new int[number_distenctcolor];
////            int[] collection_colorbluesum = new int[number_distenctcolor];
////            Dictionary<int, int> colorandreprsentivecolor = new Dictionary<int, int>();
////            foreach (var item1 in ClustersofColors)
////            {
////                HashSet<int> hass = item1;
////                counterrr = 0;
////                avrred = 0;
////                avrgreen = 0;
////                avrblue = 0;
////                finalcolor = 0;
////                foreach (var item2 in hass)
////                {
////                    collection_colorredsum[counterrr] = (byte)(item2 >> 16);
////                    collection_colorgreensum[counterrr] = (byte)(item2 >> 8);
////                    collection_colorbluesum[counterrr] = (byte)(item2);
////                    counterrr = counterrr + 1;
////                }
////                for (int c = 0; c < counterrr; c++)
////                {
////                    avrred += collection_colorredsum[counterrr];
////                    avrgreen += collection_colorgreensum[counterrr];
////                    avrblue += collection_colorbluesum[counterrr];


////                }
////                avrred = avrred / counterrr;
////                avrgreen = avrgreen / counterrr;
////                avrblue = avrblue / counterrr;
////                finalcolor = (avrred << 16) + (avrgreen << 8) + (avrblue);
////                foreach (var item2 in hass)
////                {
////                    colorandreprsentivecolor.Add(item2, finalcolor);
////                }

////                Array.Clear(collection_colorredsum, 0, counterrr);
////                Array.Clear(collection_colorgreensum, 0, counterrr);
////                Array.Clear(collection_colorbluesum, 0, counterrr);
////            }
////            return colorandreprsentivecolor;


////        }
////        // function 5
////        public static void QuantizationTheImage(RGBPixel[,] Matrixforimagepath, Dictionary<int, int> colorandreprsentivecolor)
////        {
////            int color = 0;
////            int counter_rows = GetHeight(Matrixforimagepath);
////            int counter_columns = GetWidth(Matrixforimagepath);
////            Dictionary<int, colorindecesmatrix> listcolors = new Dictionary<int, colorindecesmatrix>(counter_rows * counter_columns);
////            int counter_loop1 = 0;
////            int counter_loop2 = 0;

////            colorindecesmatrix struc = new colorindecesmatrix();
////            while (counter_loop1 < counter_rows)
////            {

////                while (counter_loop2 < counter_columns)
////                {
////                    int red = Matrixforimagepath[counter_loop1, counter_loop2].red;
////                    int blue = Matrixforimagepath[counter_loop1, counter_loop2].blue;
////                    int green = Matrixforimagepath[counter_loop1, counter_loop2].green;
////                    color = (red << 16) + (green << 8) + blue;
////                    struc.nodeindex1 = counter_loop1;
////                    struc.nodeindex2 = counter_loop2;
////                    listcolors.Add(color, struc);

////                    counter_loop2 = counter_loop2 + 1;
////                }
////                counter_loop1 = counter_loop1 + 1;

////            }
////            foreach (var item in listcolors)
////            {
////                int value = colorandreprsentivecolor[item.Key];
////                Matrixforimagepath[item.Value.nodeindex1, item.Value.nodeindex2].red = (byte)(value >> 16);
////                Matrixforimagepath[item.Value.nodeindex1, item.Value.nodeindex2].green = (byte)(value >> 8);
////                Matrixforimagepath[item.Value.nodeindex1, item.Value.nodeindex2].blue = (byte)(value);

////            }



////        }


////    }
////}
////*/


/////*
////  public static int FindDistinctColor(RGBPixel[,] ImageMatrix)
////        {
////            int R, G, B;
////            //RGBPixel[,,] d1 = new RGBPixel[256,256,256];
////            HashSet<List<int>> s = new HashSet<List<int>>();
////            List<int> d = new List<int>();
////            //HashSet<int> s = new HashSet<int>(d);
////            //int[,] d =new int[256, 256];

////            for (int j = 0; j < GetHeight(ImageMatrix); j++)
////            {
////                for (int i = 0; i < GetWidth(ImageMatrix); i++)
////                {
////                    R = ImageMatrix[j, i].red;
////                    G = ImageMatrix[j, i].green;
////                    B = ImageMatrix[j, i].blue;

////                    d.Add(R);
////                    d.Add(G);
////                    d.Add(B);
////                    s.Add(d);
////                }
////            }
////            int m = s.ToList().Count(); 
////            //List<int> ListOfDC;// = (List)s;
////            return m;
////        }
//// */  

