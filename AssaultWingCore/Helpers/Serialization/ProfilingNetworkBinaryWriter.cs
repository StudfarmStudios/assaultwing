using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Diagnostics;

namespace AW2.Helpers.Serialization
{
    // Scope Tree represents current point of execution (node per frame at root level, nodes within frame for various serialized objects)
    //
    // Registering written bytes adds them to currently active Scope Tree Node

    internal class ScopeTreeNode : IComparable
    {
        public string name;
        public uint totalBytes;
        public List<ScopeTreeNode> children = new List<ScopeTreeNode>();

        public ScopeTreeNode(string name)
        {
            this.name = name;
        }
        public void Sort()
        {
            children.Sort();
        }

        public ScopeTreeNode GetChild(string name)
        {
            foreach (ScopeTreeNode child in children)
            {
                if (child.name.Equals(name))
                {
                    return child;
                }
            }
            ScopeTreeNode newNode = new ScopeTreeNode(name);
            children.Add(newNode);
            return newNode;
        }

        public int CompareTo(object obj)
        {
            long delta = ((ScopeTreeNode)obj).totalBytes - totalBytes;
            return (int)delta;
        }

    }

    public class ProfilingNetworkBinaryWriter : NetworkBinaryWriter
    {
        private static Stack<ScopeTreeNode> currentStack;
        private static ScopeTreeNode rootNode = null;

        public ProfilingNetworkBinaryWriter(Stream output) : base(output)
        {
            if (rootNode == null)
            {
                currentStack = new Stack<ScopeTreeNode>();
                rootNode = new ScopeTreeNode("ROOT");
                currentStack.Push(rootNode);
            }
        }
        
        protected override void WriteBytes(byte[] bytes, int index, int count)
        {
            base.WriteBytes(bytes, index, count);
            
            // Record byte count on current nodes
            foreach (ScopeTreeNode stackNode in currentStack)
            {
                stackNode.totalBytes += (uint)count;
            }
        }

        public static void Push(string scopeName)
        {
            if (currentStack != null)
            {
                ScopeTreeNode newNode = currentStack.Peek().GetChild(scopeName);
                currentStack.Push(newNode);
            }
        }

        public static void Pop()
        {
            if (currentStack != null)
            {
                currentStack.Pop();
            }
        }

        internal class Counter
        {
            public uint total;
            public uint min = UInt32.MaxValue;
            public uint max = UInt32.MinValue;
            public uint count;

            public void Add(uint value)
            {
                if (value == 0)
                    return;
                
                total += value;
                count++;
                min = (value < min ? value : min);
                max = (value > max ? value : max);
            }
        }

        // Write out a report of network statistics
        // Dump all frames, sorted so the heaviest traffic comes last
        

        public static void DumpStats()
        {
            if (currentStack == null)
                return;
            

            using (StreamWriter writer = new StreamWriter("network-stats-" + Process.GetCurrentProcess().Id + ".txt"))
            {
                writer.WriteLine("=== SUMMARY PER TAG === ");

                Dictionary<string, Counter> perTag = new Dictionary<string, Counter>();
                CollectPerTagInfo(rootNode, perTag);
                foreach (var pair in perTag.OrderByDescending(x => x.Value.total))
                {
                    string name = pair.Key;
                    if (pair.Value.count > 0 )
                        writer.WriteLine(name.PadRight(20) + " Total:" + pair.Value.total + " Count:" + pair.Value.count + " (Min:" + pair.Value.min + " Avg:" + (int)(pair.Value.total / pair.Value.count) + " Max:" + pair.Value.max + ")");
                }

                writer.WriteLine("=== SCOPE TREE ===");

                Dump(rootNode, writer, 0);

                
            }
        }

        private static void CollectPerTagInfo(ScopeTreeNode node, Dictionary<string, Counter> perTagInfo)
        {
            if (perTagInfo.ContainsKey(node.name))
            {
                perTagInfo[node.name].Add(node.totalBytes);
            }
            else
            {
                Counter c = new Counter();
                c.Add(node.totalBytes);


                perTagInfo.Add(node.name, c);
            }
            foreach (ScopeTreeNode child in node.children)
            {
                CollectPerTagInfo(child, perTagInfo);
            }

        }
        private static void Dump(ScopeTreeNode node, StreamWriter writer, int depth)
        {
            for (int i = 0; i < depth; i++)
                writer.Write("    ");
            node.Sort();

            writer.WriteLine(node.name + " (" + node.totalBytes + ")");

            foreach (ScopeTreeNode child in node.children)
            {
                Dump(child, writer, depth + 1);
            }
        }
    }
}
