﻿/****************************************************************************
 * Copyright (c) 2017 maoling@putao.com
****************************************************************************/
using System.Collections.Generic;
using System.Xml;

namespace PTGame.Blockly
{
    /// <summary>
    /// Mutator for the if/else if/else block. This class modifies the block model, but is not
    /// responsible for updating the view hierarchy or showing an editor to the user.
    /// </summary>
    public class IfElseMutator : Mutator
    {
        private const string ELSE_INPUT_NAME = "ELSE";
        private const string IF_INPUT_PREFIX = "IF";
        private const string DO_INPUT_PREFIX = "DO";
        private static string CHECK = "Boolean";
        private const int ALIGN = Blockly.ALIGN_LEFT;
        
        /// <summary>
        /// The number of else if inputs on this block.
        /// </summary>
        private int mElseIfCount = 0;
        public int ElseIfCount { get { return mElseIfCount; } }
        
        /// <summary>
        /// True if this block has an else statement at the end, false otherwise.
        /// </summary>
        private bool mHasElse = false;
        public bool HasElse { get { return mHasElse; } }

        public override bool NeedEditor
        {
            get { return true; }
        }
        
        /// <summary>
        /// Convenience method for invoking a mutation event programmatically, updating the Mutator with the provided values.
        /// </summary>
        /// <param name="elseIfCount">The number of else if inputs for this block.</param>
        /// <param name="hasElse">True if this block should have a final else statement.</param>
        public void Mutate(int elseIfCount, bool hasElse)
        {
            if (elseIfCount == mElseIfCount && hasElse == mHasElse)
                return;

            mElseIfCount = elseIfCount;
            mHasElse = hasElse;
            if (mBlock != null)
            {
                UpdateInternal();
            }
        }

        protected override void OnAttached()
        {
            UpdateInternal();
        }
        
        public override XmlElement ToXml()
        {
            if (mElseIfCount <= 0 && !mHasElse)
                return null;
            
            XmlElement xmlElement = XmlUtil.CreateDom("mutation");
            if (mElseIfCount > 0)
                xmlElement.SetAttribute("elseif", mElseIfCount.ToString());

            if (mHasElse)
                xmlElement.SetAttribute("else", "1");
            
            return xmlElement;
        }

        public override void FromXml(XmlElement xmlElement)
        {
            mElseIfCount = 0;
            mHasElse = false;
            if (xmlElement.HasAttribute("elseif"))
                mElseIfCount = int.Parse(xmlElement.GetAttribute("elseif"));
            if (xmlElement.HasAttribute("else"))
                mHasElse = true;
            
            this.UpdateInternal();
        }

        /// <summary>
        /// Performs the model changes for the given count. This will reuse as many inputs as possible,
        /// creating new inputs if necessary. Leftover inputs will be disconnected and thrown away.
        /// </summary>
        private void UpdateInternal()
        {
            List<Input> oldInputs = new List<Input>(mBlock.InputList);
            List<Input> newInputs = new List<Input>();
            
            // Set aside the else input for the end.
            Input elseInput = mBlock.GetInput(ELSE_INPUT_NAME);
            if (elseInput != null)
                oldInputs.Remove(elseInput);
            
            // Move the first if/do block into the new input list
            newInputs.Add(oldInputs[0]);    //IF0
            newInputs.Add(oldInputs[1]);    //DO0
            oldInputs.RemoveRange(0, 2);
            
            // Copy over existing inputs if we have them, make new ones if we don't.
            for (int i = 1; i <= mElseIfCount; i++)
            {
                if (oldInputs.Count >= 2)
                {
                    newInputs.Add(oldInputs[0]);    //IFi
                    newInputs.Add(oldInputs[1]);    //DOi
                    oldInputs.RemoveRange(0, 2);
                }
                else
                {
                    // IFi value input
                    Input inputValue = InputFactory.Create(Blockly.INPUT_VALUE, IF_INPUT_PREFIX + i, ALIGN, new List<string>() {CHECK});
                    inputValue.AppendField(new FieldLabel(null, Blockly.Msg[MsgDefine.CONTROLS_IF_MSG_ELSEIF]));

                    // DOi statement input
                    Input inputStatement = InputFactory.Create(Blockly.NEXT_STATEMENT, DO_INPUT_PREFIX + i, ALIGN, null);
                    inputStatement.AppendField(new FieldLabel(null, Blockly.Msg[MsgDefine.CONTROLS_IF_MSG_THEN]));
                    
                    newInputs.Add(inputValue);
                    newInputs.Add(inputStatement);
                }
            }
            
            // Add the else clause if we need it
            if (mHasElse)
            {
                if (elseInput == null)
                {
                    elseInput = InputFactory.Create(Blockly.NEXT_STATEMENT, ELSE_INPUT_NAME, ALIGN, null);
                    elseInput.AppendField(new FieldLabel(null, Blockly.Msg[MsgDefine.CONTROLS_IF_MSG_ELSE]));
                }
                newInputs.Add(elseInput);
            }
            else if (elseInput != null)
            {
                // dispose the else statement
                elseInput.Dispose();
            }

            // Clean up extra inputs
            foreach (Input input in oldInputs)
                input.Dispose();
            oldInputs.Clear();
            
            mBlock.Reshape(newInputs);
        }
    }
}