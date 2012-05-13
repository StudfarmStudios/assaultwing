using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AW2.Helpers.Serialization
{
    /// <summary>
    /// Scope Tree represents current point of execution (node per frame at root level, nodes within frame for various serialized objects).
    /// Registering written bytes adds them to currently active <see cref="ScopeTreeNode"/>.
    /// </summary>
    public class ScopeTreeNode : IComparable
    {
        public string Name { get; set; }
        public uint TotalBytes { get; set; }
        public List<ScopeTreeNode> Children { get; private set; }

        public ScopeTreeNode(string name)
        {
            Name = name;
            Children = new List<ScopeTreeNode>();
        }

        public void Sort()
        {
            Children.Sort();
        }

        public ScopeTreeNode GetChild(string name)
        {
            var item = Children.FirstOrDefault(child => child.Name == name);
            if (item != null) return item;
            var newNode = new ScopeTreeNode(name);
            Children.Add(newNode);
            return newNode;
        }

        public int CompareTo(object obj)
        {
            return ((ScopeTreeNode)obj).TotalBytes.CompareTo(TotalBytes);
        }
    }

    public class ProfilingNetworkBinaryWriter : NetworkBinaryWriter
    {
        private class Counter
        {
            public uint Total;
            public uint Min = UInt32.MaxValue;
            public uint Max = UInt32.MinValue;
            public uint Count;

            public void Add(uint value)
            {
                if (value == 0) return;
                Total += value;
                Count++;
                Min = (value < Min ? value : Min);
                Max = (value > Max ? value : Max);
            }
        }

        private static Stack<ScopeTreeNode> _currentStack;
        private static ScopeTreeNode _rootNode;

        public ProfilingNetworkBinaryWriter(Stream output)
            : base(output)
        {
            if (_rootNode == null) Reset();
        }

        public static void Reset()
        {
            _currentStack = null;
            _rootNode = null;
            if (AW2.Core.AssaultWingCore.Instance == null || !AW2.Core.AssaultWingCore.Instance.Settings.Net.HeavyProfileLog) return;
            _currentStack = new Stack<ScopeTreeNode>();
            _rootNode = new ScopeTreeNode("ROOT");
            _currentStack.Push(_rootNode);
        }

        public static void Push(string scopeName)
        {
            if (_currentStack == null) return;
            var newNode = _currentStack.Peek().GetChild(scopeName);
            _currentStack.Push(newNode);
        }

        public static void Pop()
        {
            if (_currentStack == null) return;
            _currentStack.Pop();
        }

        /// <summary>
        /// Write out a report of network statistics.
        /// Dump all frames, sorted so the heaviest traffic comes first.
        /// </summary>
        public static void DumpStats()
        {
            if (_currentStack == null) return;
            var statFile = Path.Combine(AW2.Helpers.Log.LogPath, string.Format("network-stats-{0:yyyy-MM-dd HH-mm-ss}.txt", DateTime.Now));
            Log.Write("Dumping network stats to " + statFile);
            using (var writer = new StreamWriter(statFile))
            {
                writer.WriteLine("=== SUMMARY PER TAG === ");
                var perTag = new Dictionary<string, Counter>();
                CollectPerTagInfo(_rootNode, perTag);
                foreach (var pair in perTag.OrderByDescending(x => x.Value.Total))
                {
                    if (pair.Value.Count == 0) continue;
                    writer.WriteLine("{0}\tTotal={1}\tCount={2}\t(Min:{3}\tAvg:{4}\tMax:{5})",
                        pair.Key, pair.Value.Total, pair.Value.Count, pair.Value.Min, pair.Value.Total / pair.Value.Count, pair.Value.Max);
                }
                writer.WriteLine();
                writer.WriteLine("=== SCOPE TREE ===");
                Dump(_rootNode, writer, 0);
            }
        }

        protected override void WriteBytes(byte[] bytes, int index, int count)
        {
            base.WriteBytes(bytes, index, count);
            if (_currentStack == null || _currentStack.Peek().Name == null) return;

            // Record byte count on current nodes
            foreach (var stackNode in _currentStack)
                stackNode.TotalBytes += (uint)count;
        }

        private static void CollectPerTagInfo(ScopeTreeNode node, Dictionary<string, Counter> perTagInfo)
        {
            if (node.Name != null)
            {
                if (perTagInfo.ContainsKey(node.Name))
                {
                    perTagInfo[node.Name].Add(node.TotalBytes);
                }
                else
                {
                    var c = new Counter();
                    c.Add(node.TotalBytes);
                    perTagInfo.Add(node.Name, c);
                }
            }
            foreach (var child in node.Children) CollectPerTagInfo(child, perTagInfo);
        }

        private static void Dump(ScopeTreeNode node, StreamWriter writer, int depth)
        {
            for (int i = 0; i < depth; i++) writer.Write("    ");
            node.Sort();
            if (node.Name != null) writer.WriteLine(node.Name + " (" + node.TotalBytes + ")");
            foreach (ScopeTreeNode child in node.Children) Dump(child, writer, depth + 1);
        }
    }
}
