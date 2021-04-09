﻿using System;
using System.Collections.Generic;
using Foundatio.Parsers.LuceneQueries.Nodes;
using Foundatio.Parsers.LuceneQueries.Visitors;

namespace Foundatio.Parsers.LuceneQueries.Extensions {
    public static class QueryNodeExtensions {
        public static IQueryNode ReplaceSelf(this IQueryNode node, IQueryNode newValue) {
            var parent = node.Parent as GroupNode;

            if (parent == null)
                return node;

            newValue.Parent = parent;

            if (parent.Left == node)
                parent.Left = newValue;
            else if (parent.Right == node)
                parent.Right = newValue;

            return newValue;
        }

        public static void RemoveSelf(this IQueryNode node) {
            var parent = node.Parent as GroupNode;

            if (parent == null)
                return;

            if (parent.Left == node)
                parent.Left = null;
            else if (parent.Right == node)
                parent.Right = null;
        }

        public static bool IsExcluded(this IFieldQueryNode node) {
            if (node == null)
                return false;
            
            return (node.IsNegated.HasValue && node.IsNegated.Value == true) || (!String.IsNullOrEmpty(node.Prefix) && node.Prefix == "-");
        }
        
        public static bool IsRequired(this IFieldQueryNode node) {
            if (node == null)
                return false;
            
            return !String.IsNullOrEmpty(node.Prefix) && node.Prefix == "+";
        }

        public static bool IsNegated(this IFieldQueryNode node) {
            return node.IsExcluded();
        }

        public static void InvertNegation(this IFieldQueryNode node) {
            if (node.IsNegated.HasValue)
                node.IsNegated = !node.IsNegated.Value;
            else if (!String.IsNullOrEmpty(node.Prefix) && node.Prefix == "-")
                node.Prefix = null;
            else
                node.IsNegated = true;
        }

        public static bool IsNodeOrGroupNegated(this IFieldQueryNode node) {
            if (node.IsRequired())
                return false;
            
            return node.IsExcluded() || node.GetGroupNode().IsExcluded();
        }

        public static GroupNode GetRootNode(this IQueryNode node) {
            if (node == null)
                return null;

            var current = node;
            do {
                if (current.Parent == null)
                    return current as GroupNode;

                current = current.Parent;
            } while (current != null);

            return null;
        }

        public static GroupNode GetGroupNode(this IQueryNode node, bool onlyParensOrRoot = true) {
            if (node == null)
                return null;
            
            var current = node;
            do {
                if (current is GroupNode groupNode && (!onlyParensOrRoot || groupNode.HasParens || groupNode.Parent == null))
                    return groupNode;
                
                current = current.Parent;
            } while (current != null);

            return null;
        }

        private const string TimeZoneKey = "@TimeZone";
        public static string GetTimeZone(this IFieldQueryNode node, string defaultTimeZone = null) {
            if (!node.Data.TryGetValue(TimeZoneKey, out var value))
                return defaultTimeZone;

            return value as string;
        }

        public static void SetTimeZone(this IFieldQueryNode node, string timeZone) {
            node.Data[TimeZoneKey] = timeZone;
        }

        private const string OriginalFieldKey = "@OriginalField";
        public static string GetOriginalField(this IFieldQueryNode node) {
            if (!node.Data.TryGetValue(OriginalFieldKey, out var value))
                return node.Field;

            return value as string;
        }

        public static void SetOriginalField(this IFieldQueryNode node, string field) {
            node.Data[OriginalFieldKey] = field;
        }

        private const string OperationTypeKey = "@OperationType";
        public static string GetOperationType(this IQueryNode node) {
            if (!node.Data.TryGetValue(OperationTypeKey, out var value))
                return null;

            return (string)value;
        }

        public static void SetOperationType(this IQueryNode node, string operationType) {
            node.Data[OperationTypeKey] = operationType;
        }
        
        public static string[] GetDefaultFields(this IQueryNode node, string[] rootDefaultFields) {
            var scopedNode = GetGroupNode(node);
            return !String.IsNullOrEmpty(scopedNode?.Field) ? new[] { scopedNode.Field } : rootDefaultFields;
        }

        public static GroupOperator GetOperator(this IQueryNode node, IQueryVisitorContext context) {
            var defaultOperator = GroupOperator.And;
            if (context != null && context.DefaultOperator != GroupOperator.Default)
                defaultOperator = context.DefaultOperator;

            var groupNode = node as GroupNode;
            if (groupNode == null)
                groupNode = node.Parent as GroupNode;

            if (groupNode == null)
                return defaultOperator;

            switch (groupNode.Operator) {
                case GroupOperator.And:
                    return GroupOperator.And;
                case GroupOperator.Or:
                    return GroupOperator.Or;
                default:
                    return defaultOperator;
            }
        }

        private const string ReferencedFieldsKey = "@ReferencedFields";
        private const string CurrentGroupReferencedFieldsKey = "@CurrentGroupReferencedFields";
        public static ISet<string> GetReferencedFields<T>(this T node, IQueryVisitorContext context = null, bool currentGroupOnly = false) where T : IQueryNode {
            if (!currentGroupOnly && node.Data.TryGetValue(ReferencedFieldsKey, out var allFieldsObject) && allFieldsObject is ISet<string> allFields)
                return allFields;

            if (currentGroupOnly && node.Data.TryGetValue(CurrentGroupReferencedFieldsKey, out var immediateFieldsObject) && immediateFieldsObject is ISet<string> immediateFields)
                return immediateFields;

            var fields = new HashSet<string>();
            GatherReferencedFields(context, node, fields, 0, currentGroupOnly ? 0 : -1);

            if (!currentGroupOnly)
                node.Data[ReferencedFieldsKey] = fields;

            if (currentGroupOnly)
                node.Data[CurrentGroupReferencedFieldsKey] = fields;

            return fields;
        }

        private static void GatherReferencedFields(IQueryVisitorContext context, IQueryNode node, HashSet<string> fields, int currentGroupDepth, int maxGroupDepth) {
            if (maxGroupDepth >= 0 && currentGroupDepth >= 0 && currentGroupDepth > maxGroupDepth)
                return;

            if (node is IFieldQueryNode fieldNode) {
                if (fieldNode.Field != null) {
                    fields.Add(fieldNode.Field);
                } else if (fieldNode is not GroupNode) {
                    var defaultFields = node.GetDefaultFields(context?.DefaultFields);
                    if (defaultFields == null || defaultFields.Length == 0)
                        fields.Add("");
                    else
                        foreach (string defaultField in fields)
                            fields.Add(defaultField);
                }
            }

            if (node is GroupNode groupNode) {
                if (groupNode.HasParens)
                    currentGroupDepth++;

                if (groupNode.Left != null)
                    GatherReferencedFields(context, groupNode.Left, fields, currentGroupDepth, maxGroupDepth);

                if (groupNode.Right != null)
                    GatherReferencedFields(context, groupNode.Right, fields, currentGroupDepth, maxGroupDepth);
            }
        }
    }
}