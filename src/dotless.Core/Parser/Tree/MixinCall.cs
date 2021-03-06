namespace dotless.Core.Parser.Tree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exceptions;
    using Infrastructure;
    using Infrastructure.Nodes;
    using Utils;

    public class MixinCall : Node
    {
        public NodeList<Expression> Arguments { get; set; }
        protected Selector Selector { get; set; }

        public MixinCall(NodeList<Element> elements, NodeList<Expression> arguments)
        {
            Selector = new Selector(elements);
            Arguments = arguments;
        }

        public override Node Evaluate(Env env)
        {
            var found = false;
            var closures = env.FindRulesets(Selector);

            if(closures == null)
                throw new ParsingException(Selector.ToCSS(env).Trim() + " is undefined", Index);

            env.Rule = this;

            var rules = new NodeList();
            foreach (var closure in closures)
            {
                var ruleset = closure.Ruleset;

                if (!ruleset.MatchArguments(Arguments, env))
                    continue;

                found = true;

                if (ruleset is MixinDefinition)
                {
                    try
                    {
                        var mixin = ruleset as MixinDefinition;
                        rules.AddRange(mixin.Evaluate(Arguments, env, closure.Context).Rules);
                    }
                    catch (ParsingException e)
                    {
                        throw new ParsingException(e.Message, e.Index, Index);
                    }
                }
                else
                {
                    if (ruleset.Rules != null)
                    {
                        var nodes = new List<Node>(ruleset.Rules);
                        NodeHelper.ExpandNodes<MixinCall>(env, nodes);

                        rules.AddRange(nodes);
                    }
                }
            }

            env.Rule = null;

            if (!found)
            {
                var message = String.Format("No matching definition was found for `{0}({1})`",
                                            Selector.ToCSS(env).Trim(),
                                            StringExtensions.JoinStrings(Arguments.Select(a => a.ToCSS(env)), ", "));
                throw new ParsingException(message, Index);
            }

            return rules;
        }
    }
}