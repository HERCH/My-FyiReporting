/* ====================================================================
   Copyright (C) 2004-2008  fyiReporting Software, LLC
   Copyright (C) 2011  Peter Gill <peter@majorsilence.com>

   This file is part of the fyiReporting RDL project.
	
   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.


   For additional information, email info@fyireporting.com or visit
   the website www.fyiReporting.com.
*/
using System;
using System.ComponentModel;            // need this for the properties metadata
using System.Globalization;

namespace Majorsilence.Reporting.RdlDesign
{
    /// <summary>
    /// PropertyExpr - 
    /// </summary>
    [TypeConverter(typeof(PropertyMarginConverter))]
    internal class PropertyMargin
    {
        PropertyReport _pr;

        public PropertyMargin(PropertyReport pr)
        {
            _pr = pr;
        }

        [RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Margin_Left")]
        public string Left
        {
            get
            {
                return _pr.GetReportValue("LeftMargin");
            }
            set
            {
                SetMargin("LeftMargin", value);
            }
        }

		[LocalizedDisplayName("Margin_Right")]
		[RefreshProperties(RefreshProperties.Repaint)]
        public string Right
        {
            get
            {
                return _pr.GetReportValue("RightMargin");
            }
            set
            {
                SetMargin("RightMargin", value);
            }
        }

		[RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Margin_Top")]
		public string Top
        {
            get
            {
                return _pr.GetReportValue("TopMargin");
            }
            set
            {
                SetMargin("TopMargin", value);
            }
        }

		[RefreshProperties(RefreshProperties.Repaint)]
		[LocalizedDisplayName("Margin_Bottom")]
		public string Bottom
        {
            get
            {
                return _pr.GetReportValue("BottomMargin");
            }
            set
            {
                SetMargin("BottomMargin", value);
            }
        }

        void SetMargin(string l, string v)
        {
            DesignerUtility.ValidateSize(v, true, false);
            _pr.SetReportValue(l, v);
        }

    }

    internal class PropertyMarginConverter : ExpandableObjectConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context,
                                         System.Type destinationType)
        {
            if (destinationType == typeof(string))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, 
            CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is PropertyMargin)
            {
                PropertyMargin pe = value as PropertyMargin;
                return string.Format("(l={0}, t={1}, r={2}, b={3})", pe.Left, pe.Top, pe.Right, pe.Bottom);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

    }
}