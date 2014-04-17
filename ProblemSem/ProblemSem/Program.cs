using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            string folder = @"..\";
            string[] graphs = File.ReadAllLines(folder + "egos.txt");
            string[] resultNb = new string[graphs.Length];
            string[] resultLabels = new string[graphs.Length];
            for (int i = 0; i < graphs.Length; i++)
            {
                SolveGraph(graphs[i], out resultNb[i], out resultLabels[i]);
            }
            File.WriteAllLines(folder + "minSelecteds.txt", resultNb);
            File.WriteAllLines(folder + "opinions.txt", resultLabels);
        }

        private static void SolveGraph(string ingraph, out string nb, out string labels)
        {
            //graph of lenght 1
            if (ingraph.Length == 1)
            {
                nb = "1";
                labels = "1";
                return;
            }

            //initialization and input loading
            nb = "";
            labels = "";
            List<int[]> graphs = new List<int[]>();
            char[] originalChar = ingraph.ToCharArray();
            int[] originalInput = originalChar.Select(ch => int.Parse(ch.ToString())).ToArray();

            PreprocessGraph(originalInput, graphs);

            //solve each graph from preprocessing separately
            List<bool[]> midoptimums = new List<bool[]>();
            foreach (int[] graph in graphs)
            {
                if (graph.Length > 0)
                    midoptimums.Add(GetOptimalLabeling(graph));
                else//for tests only
                    if (graphs.IndexOf(graph) != 0 && graphs.IndexOf(graph) != graphs.Count - 1)
                    {
                        throw new InvalidOperationException("A Graph with zero lenght not at the beginning or end");
                    }
            }

            bool[] optimum = Finalize(originalInput, graphs, midoptimums);

            //save results
            nb = optimum.Where(op => op).Count().ToString();
            labels = string.Concat(optimum.Select(op => (Convert.ToInt32(op)).ToString()).Cast<string>());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ingraph">original input graph</param>
        /// <param name="graphs">graphs after preprocessing</param>
        /// <param name="midoptimums">partial results</param>
        /// <returns></returns>
        private static bool[] Finalize(int[] ingraph, List<int[]> graphs, List<bool[]> midoptimums)
        {
            if (graphs.Count == 1 && ingraph.Length == midoptimums.FirstOrDefault().Length)
                return midoptimums.FirstOrDefault();

            bool[] result = new bool[ingraph.Length];
            bool lastOne = false;
            int nextToUse = 0;
            for (int i = 0; i < ingraph.Length; i++)
            {
                if (ingraph[i] == 1)
                {
                    if (lastOne)
                    {
                        result[i - 1] = true;
                        while (i < ingraph.Length && ingraph[i] == 1)//pass at least once
                        {
                            result[i++] = true;
                        }
                        i--;
                        lastOne = false;
                    }
                    else
                        lastOne = true;
                }
                else
                {
                    if (lastOne)
                        i--;
                    lastOne = false;
                    for (int j = 0; j < midoptimums[nextToUse].Length - 1; j++)
                    {
                        result[i++] = midoptimums[nextToUse][j];
                    }
                    if (ingraph[i] == 3 && (ingraph.Length <= i + 2 || ingraph[i + 1] != 1 || ingraph[i + 2] != 1))
                        i--;
                    else
                        result[i] = midoptimums[nextToUse][midoptimums[nextToUse].Length - 1];

                    nextToUse++;
                }
            }

            return result;
        }

        /// <summary>
        /// Solve one graph with egos one or two, no consecutive ones
        /// </summary>
        /// <param name="graph"></param>
        /// <returns></returns>
        private static bool[] GetOptimalLabeling(int[] graph)
        {
            ResultPair[] dynamic = new ResultPair[graph.Length];
            if (IsEnd(Ending.TwoOne, graph, 0))
            {
                SolveSinglePart(dynamic, graph);
                return GetResultFromDynamic(dynamic);
            }

            SolveFirstPart(graph, dynamic);

            //main loop of dynamic programming
            for (int l = 0; l < dynamic.Length; l++)
            {
                if (dynamic[l] != null)
                {
                    SolveOnePart(dynamic, graph, l);
                    // for testing only
                    for (int j = l; j < dynamic.Length; j++)
                    {
                        if (dynamic[j] != null && dynamic[j].Labeling.Count != j + 1)
                        {
                            throw new InvalidOperationException("nesprávně dlouhé labeling");
                        }
                    }
                }
            }

            return GetResultFromDynamic(dynamic);
        }


        private static void SolveFirstPart(int[] graph, ResultPair[] dynamic)
        {
            int i = 0;
            if (graph[0] == 2)
            {
                int count = 0;
                List<bool> labeling = new List<bool>();
                ChooseEverySecondTwo(graph, labeling, ref count, ref i, 0);

                dynamic[i] = new ResultPair
                {
                    Optimum = count,
                    Labeling = labeling,
                    Ending = i == 0 ? Ending.DoubleTwo : Ending.SingleTwo
                };
            }
            else
            {//unchosen one at the beginning
                //starter 12
                int count = 2;
                i = 2;
                List<bool> labeling = new List<bool>();
                labeling.Add(true);
                labeling.Add(true);
                ChooseEverySecondTwo(graph, labeling, ref count, ref i, 1);

                if (dynamic[i] == null || dynamic[i].Optimum > count)
                    dynamic[i] = new ResultPair
                    {
                        Optimum = count,
                        Labeling = labeling,
                        Ending = i == 1 ? Ending.OneTwo : Ending.SingleTwo,
                    };
                //starters 22
                i = 1;
                while (i < graph.Length && graph[i] == 2 && graph[i + 1] == 2)
                {
                    labeling = new List<bool>();
                    count = 2;
                    labeling.Add(true);
                    labeling.Add(true);
                    ChooseEverySecondTwoDown(labeling, ref count, i - 1, 0, 2 - i);
                    int j = i + 2;
                    ChooseEverySecondTwo(graph, labeling, ref count, ref j, 1 - i);
                    
                    if (dynamic[j] == null || dynamic[j].Optimum > count)
                        dynamic[j] = new ResultPair
                        {
                            Optimum = count,
                            Labeling = labeling,
                            Ending = j == i + 1 ? Ending.DoubleTwo : Ending.SingleTwo,
                        };
                    i++;
                }

                //starter 21
                i = 1;
                while (graph[i] != 2 || graph[i + 1] != 1)
                {
                    i++;
                }
                labeling = new List<bool>();
                labeling.Add(true);
                labeling.Add(true);
                count = 2;
                ChooseEverySecondTwoDown(labeling, ref count, i - 1, 0, 2 - i);
                if (dynamic[i + 1] == null || dynamic[i + 1].Optimum > count)
                    dynamic[i + 1] = new ResultPair
                    {
                        Optimum = count,
                        Labeling = labeling,
                        Ending = Ending.TwoOne,
                    };
            }
        }

        /// <summary>
        /// Graph consists of only one part
        /// </summary>
        /// <param name="dynamic"></param>
        /// <param name="graph"></param>
        private static void SolveSinglePart(ResultPair[] dynamic, int[] graph)
        {
            int last = graph.Length - 1;
            bool wasReverted = false;
            List<bool> labeling = new List<bool>();
            int count = 0;
            if (graph[0] == 2 && graph[last] == 2)
            {//only egos 2
                int i = 0;
                ChooseEverySecondTwo(graph, labeling, ref count, ref i, 0);
                if (labeling.Count != graph.Length)
                {
                    labeling.Add(true);
                    count++;
                }
                dynamic[last] = new ResultPair
                {
                    Optimum = count,
                    Labeling = labeling,
                    Ending = Ending.DoubleTwo,
                };
                return;
            }
            if (graph[last] == 2)
            {//two only at the end => reverse and so two at the beginning
                graph = graph.Reverse().ToArray();
                wasReverted = true;
            }
            if (graph[0] == 2)
            {//2 at the beginning, maybe reversed
                int i = 0;
                ChooseEverySecondTwo(graph, labeling, ref count, ref i, 0);
                if (graph.Length - labeling.Count > 1)
                    labeling.Add(!labeling.Last());
                //choose last one
                if (labeling.LastOrDefault())
                    labeling.Add(false);
                else
                {
                    labeling.Add(true);
                    count++;
                }
                if (wasReverted)
                {
                    graph = graph.Reverse().ToArray();
                    labeling.Reverse();
                }
                dynamic[last] = new ResultPair
                {
                    Optimum = count,
                    Labeling = labeling,
                    Ending = Ending.SingleTwo,
                };
                return;
            }
            //start and ends with ego 1
            //length of the graph is odd => starter 12 at the beginning (e.g.)
            if (graph.Length % 2 == 1)
            {
                labeling.Add(true);
                labeling.Add(true);
                count = 2;
                int i = 2;
                ChooseEverySecondTwo(graph, labeling, ref count, ref i, 1);
                //choose last one
                if (labeling.LastOrDefault())
                    labeling.Add(false);
                else
                {
                    labeling.Add(true);
                    count++;
                }
                dynamic[last] = new ResultPair
                {
                    Optimum = count,
                    Labeling = labeling,
                    Ending = Ending.SingleTwo,
                };
                return;
            }
            //legth of the graph is even => starter first 22, legth is at least 4 (or 11 => not possible)
            labeling.Add(false);
            labeling.Add(true);
            labeling.Add(true);
            count = 2;
            int j = 3;
            ChooseEverySecondTwo(graph, labeling, ref count, ref j, 0);//choose last one
            if (labeling.LastOrDefault())
                labeling.Add(false);
            else
            {
                labeling.Add(true);
                count++;
            }
            dynamic[last] = new ResultPair
            {
                Optimum = count,
                Labeling = labeling,
                Ending = Ending.SingleTwo,
            };
        }

        private static bool[] GetResultFromDynamic(ResultPair[] dynamic)
        {
            return dynamic[dynamic.Length - 1].Labeling.ToArray();
        }

        /// <summary>
        /// Solve one part, it is not single or first part
        /// </summary>
        /// <param name="dynamic"></param>
        /// <param name="graph"></param>
        /// <param name="l"></param>
        private static void SolveOnePart(ResultPair[] dynamic, int[] graph, int l)
        {
            if (IsEnd(dynamic[l].Ending, graph, l))
                SolveEndPart(dynamic, graph, l);
            else
                SolveMiddlePart(dynamic, graph, l);
        }

        /// <summary>
        /// Solve a middle part (start and ends with one, part before exists and is solved)
        /// </summary>
        /// <param name="dynamic"></param>
        /// <param name="graph"></param>
        /// <param name="l"></param>
        private static void SolveMiddlePart(ResultPair[] dynamic, int[] graph, int l)
        {
            ResultPair lastEnd = dynamic[l];
            if (lastEnd.Ending == Ending.TwoOne)
            {//last chosen is 1
                //starter 12, 1 is already chosen
                List<bool> labeling = new List<bool>();
                int count = 0;
                int i = l + 1;
                ChooseEverySecondTwo(graph, labeling, ref count, ref i, 1);
                SetDynamic(dynamic, i, l, count, labeling, i == l + 1 ? Ending.TwoOneTwo : Ending.SingleTwo);

                //starter 22
                i = l + 1;
                while (graph[i] == 2 && graph[i + 1] == 2)
                {
                    labeling = new List<bool>();
                    labeling.Add(true);
                    labeling.Add(true);
                    count = 2;
                    ChooseEverySecondTwoDown(labeling, ref count, i -1,l,2-i);
                    int j = i + 2;
                    ChooseEverySecondTwo(graph, labeling, ref count, ref j, 1 - i);
                    SetDynamic(dynamic, j, l, count, labeling,j == i + 1 ? Ending.DoubleTwo : Ending.SingleTwo);
                    i++;
                }
                //starter 21
                i = l + 1;
                while (graph[i] != 2 || graph[i + 1] != 1)
                {
                    i++;
                }
                labeling = new List<bool>();
                labeling.Add(true);
                labeling.Add(true);
                count = 2;
                ChooseEverySecondTwoDown(labeling, ref count, i - 1, l + 1, 2 - i);
                SetDynamic(dynamic, i + 1, l, count, labeling, Ending.TwoOne);
            }//Ending.TwoOne
            else
            {
                //starter 12
                int i = l + 1;
                List<bool> labeling = new List<bool>();
                int count = 1;
                if (graph[i] == 2)
                {
                    i++;
                    labeling.Add(false);
                }
                //nyní graph[i] == 1
                labeling.Add(true);
                int onePosition = i;
                i++;
                ChooseEverySecondTwo(graph, labeling, ref count, ref i, 1 - onePosition);
                SetDynamic(dynamic, i,l,count,labeling,i == onePosition + 1
                            ? (onePosition == l + 1 ? Ending.TwoOneTwo : Ending.OneTwo)
                            : Ending.SingleTwo);
                //starter 22
                i = l + 1;
                bool twoBeforeOne = false;
                if (graph[i] == 2)
                {
                    twoBeforeOne = true;
                    i++;
                }
                i++;
                bool makeGap = false;
                if (lastEnd.Ending == Ending.DoubleTwo ||
                        lastEnd.Ending == Ending.TwoOneTwo)
                {//make a gap 21 if it is possible
                    makeGap = true;
                }
                while (graph[i] == 2 && graph[i + 1] == 2)
                {
                    labeling = new List<bool>();
                    labeling.Add(true);
                    labeling.Add(true);
                    count = 2;
                    ChooseEverySecondTwoDown(labeling, ref count, i - 1, onePosition + 1, 2 - i);
                    labeling.Reverse();
                    //ćonnect to the previous part
                    if (twoBeforeOne)
                    {
                        if (!makeGap)
                        {
                            labeling.Add(true);
                            count++;
                            labeling.Add(false);
                        }
                        else
                        {
                            if (!labeling.Last())
                            {
                                labeling[labeling.Count - 1] = true;
                                count++;
                            }
                            labeling.Add(false);
                            labeling.Add(false);
                        }
                    }
                    else
                    {
                        if (i == onePosition + 2)
                        {
                            labeling.Add(false);
                        }
                        else
                        {
                            if (!labeling.Last())
                            {
                                labeling[labeling.Count - 1] = true;
                                count++;
                            }
                            labeling.Add(false);
                        }
                    }
                    labeling.Reverse();
                    int j = i + 2;
                    ChooseEverySecondTwo(graph, labeling, ref count, ref j, 1 - i);
                    SetDynamic(dynamic, j, l, count, labeling, j == i + 1 ? Ending.DoubleTwo : Ending.SingleTwo);
                    i++;
                }

                //starter 21
                i = l + 1;
                if (twoBeforeOne)
                    i++;
                i++;
                while (graph[i] != 2 || graph[i + 1] != 1)
                {
                    i++;
                }
                labeling = new List<bool>();
                labeling.Add(true);
                labeling.Add(true);
                count = 2;
                ChooseEverySecondTwoDown(labeling, ref count, i - 1, onePosition + 1, 2 - i);
                labeling.Reverse();
                //connect to the previous part
                if (twoBeforeOne)
                {
                    if (!makeGap)
                    {
                        labeling.Add(true);
                        count++;
                        labeling.Add(false);
                    }
                    else
                    {
                        if (!labeling.Last())
                        {
                            labeling[labeling.Count - 1] = true;
                            count++;
                        }
                        labeling.Add(false);
                        labeling.Add(false);
                    }
                }
                else
                {
                    if (i == onePosition + 2)
                    {//want to make a gap 12, so must create 212 and solve the next part
                        if (i + 2 < graph.Length)
                        {
                            bool[] helpLabeling = new bool[labeling.Count];
                            labeling.CopyTo(helpLabeling);
                            List<bool> innerLabeling = helpLabeling.ToList();
                            innerLabeling.Add(false);
                            innerLabeling.Reverse();
                            innerLabeling.Add(true);
                            int innerCount = count + 1;
                            int m = i + 3;
                            ChooseEverySecondTwo(graph, innerLabeling, ref innerCount, ref m, i + 2);
                            SetDynamic(dynamic, m, l, innerCount, innerLabeling, 
                                m == i + 2 ? Ending.TwoOneTwo : Ending.SingleTwo);
                        }
                    }
                    //try also not prolonged version
                    if (!labeling.Last())
                    {
                        labeling[labeling.Count - 1] = true;
                        count++;
                    }
                    labeling.Add(false);
                }
                labeling.Reverse();
                SetDynamic(dynamic, i + 1, l, count, labeling, Ending.TwoOne);

            }


        }

        private static void SolveEndPart(ResultPair[] dynamic, int[] graph, int l)
        {
            if (l == graph.Length - 1)
                return;
            ResultPair lastEnd = dynamic[l];
            int i = 0;
            int count = 0;
            List<bool> labeling = new List<bool>();
            if (graph[graph.Length - 1] == 2)
            {
                if (lastEnd.Ending == Ending.TwoOne)
                {
                    i = graph.Length - 1;
                    labeling = new List<bool>();
                    labeling.Add(true);
                    count = 1;
                    ChooseEverySecondTwoDown(labeling, ref count, i - 1, l + 1, 2 - i);
                    SetDynamic(dynamic, i, l, count, labeling, Ending.SingleTwo);
                    i++;

                }

                //starter last 2
                count = 1;
                labeling = new List<bool>();
                labeling.Add(true);
                bool twoBeforeOne = false;
                i = l + 1;
                if (graph[i] == 2)
                {
                    twoBeforeOne = true;
                    i++;
                }
                int onePosition = i;
                i = graph.Length - 1;
                bool makeGap = false;
                if (lastEnd.Ending == Ending.DoubleTwo ||
                        lastEnd.Ending == Ending.TwoOneTwo)
                {//make a gap 21 if it is possible
                    makeGap = true;
                }
                ChooseEverySecondTwoDown(labeling, ref count, i - 1, onePosition + 1, 2 - i);
                labeling.Reverse();
                //connect to the previous part
                //maybe make a gap 21
                if (twoBeforeOne)
                {
                    if (!makeGap)
                    {
                        labeling.Add(true);
                        count++;
                        labeling.Add(false);
                    }
                    else
                    {
                        if (!labeling.Last())
                        {
                            labeling[labeling.Count - 1] = true;
                            count++;
                        }
                        labeling.Add(false);
                        labeling.Add(false);
                    }
                }
                else
                {
                    if (i == onePosition + 2)
                    {
                        labeling.Add(false);
                    }
                    else
                    {
                        if (!labeling.Last())
                        {
                            labeling[labeling.Count - 1] = true;
                            count++;
                        }
                        labeling.Add(false);
                    }
                }
                labeling.Reverse();
                SetDynamic(dynamic, i, l, count, labeling, Ending.DoubleTwo);
                return;
            }

            SolveMiddlePart(dynamic, graph, l);
            //if there is 1 at the end, it is solved by standart method and finished here
            if (graph[graph.Length - 3] == 1)
            {//the last part ..121, already started
                if (dynamic[graph.Length - 1] == null || dynamic[graph.Length - 1].Optimum > dynamic[graph.Length - 2].Optimum)
                {//if the starter is 12 => last 1 is not chosen, propagate value only
                    labeling = new List<bool>();
                    labeling.Add(false);
                    dynamic[graph.Length - 1] = new ResultPair
                    {
                        Optimum = dynamic[graph.Length - 2].Optimum,
                        Labeling = dynamic[graph.Length - 2].Labeling.Concat(labeling).ToList(),
                        Ending = Ending.SingleTwo,//nonsens - technical only
                    };
                }
                return;
            }
            // last part, at least two 2 (..221)
            if (dynamic[graph.Length - 3] != null)
            {
                if (dynamic[graph.Length - 1] == null || dynamic[graph.Length - 1].Optimum > dynamic[graph.Length - 3].Optimum + 1)
                {
                    labeling = new List<bool>();
                    labeling.Add(false);
                    labeling.Add(true);
                    dynamic[graph.Length - 1] = new ResultPair
                    {
                        Optimum = dynamic[graph.Length - 3].Optimum + 1,
                        Labeling = dynamic[graph.Length - 3].Labeling.Concat(labeling).ToList(),
                        Ending = Ending.SingleTwo,//nonsens - technical only
                    };
                }
            }
            if (dynamic[graph.Length - 2] != null)
            {
                if (dynamic[graph.Length - 1] == null || dynamic[graph.Length - 1].Optimum > dynamic[graph.Length - 2].Optimum)
                {
                    labeling = new List<bool>();
                    labeling.Add(false);
                    dynamic[graph.Length - 1] = new ResultPair
                    {
                        Optimum = dynamic[graph.Length - 3].Optimum,
                        Labeling = dynamic[graph.Length - 3].Labeling.Concat(labeling).ToList(),
                        Ending = Ending.SingleTwo,//nonsens - technical only
                    };
                }
            }


        }

        private static void ChooseEverySecondTwoDown(List<bool> labeling, ref int count, int start, int lowerBound, int toAdd)
        {
            for (int k = start; k >= lowerBound; k--)
            {
                if ((k + toAdd) % 2 == 0)
                {
                    labeling.Add(true);
                    count++;
                }
                else
                    labeling.Add(false);
            }
            labeling.Reverse();
        }

        private static void ChooseEverySecondTwo(int[] graph, List<bool> labeling, ref int count, ref int i, int toAdd)
        {
            while (graph.Length > i && graph[i] == 2)
            {
                if ((i + toAdd) % 2 == 0)
                {
                    labeling.Add(true);
                    count++;
                }
                else
                {
                    labeling.Add(false);
                }
                i++;
            }
            i--;
            if ((i + toAdd) % 2 != 0)
            {
                i--;
                labeling.RemoveAt(labeling.Count - 1);
            } 
        }

        private static void SetDynamic(ResultPair[] dynamic, int i, int l, int count, List<bool> labeling, Ending ending)
        {
            if (dynamic[i] == null || dynamic[i].Optimum > count + dynamic[l].Optimum ||
                (dynamic[i].Optimum == count + dynamic[l].Optimum && IsStrong(ending)))
                dynamic[i] = new ResultPair
                {
                    Optimum = count + dynamic[l].Optimum,
                    Labeling = dynamic[l].Labeling.Concat(labeling).ToList(),
                    Ending = ending,
                };
        }

        /// <summary>
        /// return true, the next part, which will be solved, is an ending (bounded or unbounded)part
        /// </summary>
        /// <param name="ending"></param>
        /// <param name="graph"></param>
        /// <param name="l"></param>
        /// <returns></returns>
        private static bool IsEnd(Ending ending, int[] graph, int l)
        {
            bool endWithOne = graph[graph.Length - 1] == 1;

            if (ending == Ending.TwoOne)
            {
                int i = l + 1;
                while (i < graph.Length && graph[i] == 2)
                    i++;
                return i == graph.Length || (i == graph.Length - 1 && endWithOne);
            }

            int j = l;
            bool wasOne = false;
            while (j < graph.Length)
            {
                if (graph[j] == 1)
                {
                    if (wasOne)
                        return j == graph.Length - 1;
                    wasOne = true;
                }
                j++;
            }
            return true;
        }

        private static bool IsStrong(Ending end)
        {
            return end == Ending.DoubleTwo || end == Ending.TwoOneTwo;
        }

        /// <summary>
        /// Cut out vertices with ego greater than 2 and consecutive ones
        /// </summary>
        /// <param name="ingraph"></param>
        /// <param name="result"></param>
        /// <param name="originals"></param>
        private static void PreprocessGraph(int[] original, List<int[]> result)
        {
            List<int[]> midresult = new List<int[]>();

            //cut out vertices with ego greater than 2
            int lastCut = 0;

            for (int i = 1; i < original.Length; i++)
            {
                if (original[i] > 2)
                {
                    lastCut = RepairThrees(original, midresult, lastCut, i);
                }
            }
            if (lastCut != original.Length - 1)
            {
                RepairThrees(original, midresult, lastCut, original.Length - 1);
            }

            //remove more 111
            for (int j = 0; j < midresult.Count; j++)
            {
                bool lastOne = false;
                lastCut = 0;
                for (int i = 0; i < midresult[j].Length; i++)
                {
                    if (midresult[j][i] == 1 && lastOne)
                    {
                        lastCut = RepairOnes(midresult[j], result, lastCut, i - 2);
                        i = lastCut;
                        lastOne = false;
                    }
                    else if (midresult[j][i] == 1)
                    {
                        lastOne = true;
                    }
                    else if (lastOne)
                        lastOne = false;
                }

                if (lastCut != midresult[j].Length - 1)
                {
                    RepairOnes(midresult[j], result, lastCut, midresult[j].Length - 1);
                }
            }

        }

        /// <summary>
        /// Cut out more consecutive ones
        /// </summary>
        /// <param name="ingraph"></param>
        /// <param name="result"></param>
        /// <param name="lastCut"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static int RepairOnes(int[] ingraph, List<int[]> result, int lastCut, int i)
        {
            if (i < 0)
            {
                i = 0;
                while (i < ingraph.Length && ingraph[i] == 1)
                    i++;
                lastCut = i;
                return lastCut;
            }
            int[] original = new int[i - lastCut + 1];
            Array.Copy(ingraph, lastCut, original, 0, i - lastCut + 1);
            int[] repair = new int[original.Length];
            original.CopyTo(repair, 0);
            result.Add(repair);
            i++;
            while (i < ingraph.Length && ingraph[i] == 1)
                i++;
            lastCut = i;
            return lastCut;
        }

        /// <summary>
        /// Cut out threes
        /// </summary>
        /// <param name="ingraph"></param>
        /// <param name="result"></param>
        /// <param name="lastCut"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        private static int RepairThrees(int[] ingraph, List<int[]> result, int lastCut, int i)
        {
            int[] repair = new int[i - lastCut + 1];
            Array.Copy(ingraph, lastCut, repair, 0, i - lastCut + 1);
            repair.CopyTo(repair, 0);
            if (repair[0] > 2)
                repair[0] = 2;
            if (repair[repair.Length - 1] > 2)
                repair[repair.Length - 1] = 2;
            result.Add(repair);
            lastCut = i;
            return lastCut;
        }
    }

    class ResultPair
    {
        public int Optimum;
        public List<bool> Labeling;
        public Ending Ending;
    }

    enum Ending
    {
        SingleTwo,
        DoubleTwo,
        OneTwo,
        TwoOne,
        TwoOneTwo,
    }
}
