/* *
 * 
 * MIT License
 * 
 * Copyright(c) 2017 bitcake
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * 
 * Button attribute from the BitStrap collection: https://github.com/bitcake/bitstrap
 * 
 * */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class BehaviourButtonsHelper
{
    private static object[] emptyParamList = new object[0];

    private IList<MethodInfo> methods = new List<MethodInfo>();
    private Object targetObject;

    public void Init( Object targetObject )
    {
        this.targetObject = targetObject;
        methods = targetObject.GetType().GetMethods( BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance ).Where( m =>
            m.GetCustomAttributes( typeof( ButtonAttribute ), false ).Length == 1 &&
            m.GetParameters().Length == 0 &&
            !m.ContainsGenericParameters
        ).ToList();
    }

    public void DrawButtons()
    {
        if( methods.Count > 0 )
        {
            ShowMethodButtons();
        }
    }

    private void ShowMethodButtons()
    {
        foreach( MethodInfo method in methods )
        {
            string buttonText = ObjectNames.NicifyVariableName( method.Name );
            if( GUILayout.Button( buttonText ) )
            {
                method.Invoke( targetObject, emptyParamList );
            }
        }
    }
}

[CustomEditor( typeof( MonoBehaviour ), true, isFallback = true )]
[CanEditMultipleObjects]
public class BehaviourButtonsEditor : Editor
{
    private BehaviourButtonsHelper helper = new BehaviourButtonsHelper();

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        helper.DrawButtons();
    }

    private void OnEnable()
    {
        helper.Init( target );
    }
}
