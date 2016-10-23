// created by jay 0.7 (c) 1998 Axel.Schreiner@informatik.uni-osnabrueck.de

#line 1 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"

//
// Mono::ILASM::ILParser
// 
// (C) Sergey Chaban (serge@wildwestsoftware.com)
// (C) 2003 Jackson Harper, All rights reserved
//

using PEAPI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

using MIPermission = Mono.ILASM.Permission;
using MIPermissionSet = Mono.ILASM.PermissionSet;

namespace Mono.ILASM {

	public class ILParser {

		private CodeGen codegen;

		private bool is_value_class;
		private bool is_enum_class;
                private bool pinvoke_info;
                private string pinvoke_mod;
                private string pinvoke_meth;
                private PEAPI.PInvokeAttr pinvoke_attr;
                private ILTokenizer tokenizer;
		static int yacc_verbose_flag;
		KeyValuePair<string, TypeAttr> current_extern;

                class NameValuePair {
                        public string Name;
                        public object Value;

                        public NameValuePair (string name, object value)
                        {
                                this.Name = name;
                                this.Value = value;
                        }
                }

                class PermPair {
                        public PEAPI.SecurityAction sec_action;
                        public object perm;

                        public PermPair (PEAPI.SecurityAction sec_action, object perm)
                        {
                                this.sec_action = sec_action;
                                this.perm = perm;
                        }
                }

                public bool CheckSecurityActionValidity (System.Security.Permissions.SecurityAction action, bool for_assembly)
                {
                        if ((action == System.Security.Permissions.SecurityAction.RequestMinimum || 
                                action == System.Security.Permissions.SecurityAction.RequestOptional || 
                                action == System.Security.Permissions.SecurityAction.RequestRefuse) && !for_assembly) {
                                Report.Warning (String.Format ("System.Security.Permissions.SecurityAction '{0}' is not valid for this declaration", action));
                                return false;
                        }

                        return true;
                }

		public void AddSecDecl (object perm, bool for_assembly)
		{
			PermPair pp = perm as PermPair;

			if (pp == null) {
				MIPermissionSet ps_20 = (MIPermissionSet) perm;
				codegen.AddPermission (ps_20.SecurityAction, ps_20);
				return;
			}

			if (!CheckSecurityActionValidity ((System.Security.Permissions.SecurityAction) pp.sec_action, for_assembly))
				Report.Error (String.Format ("Invalid security action : {0}", pp.sec_action));

			codegen.AddPermission (pp.sec_action, pp.perm);
		}

                public object ClassRefToObject (object class_ref, object val)
                {
                        ExternTypeRef etr = class_ref as ExternTypeRef;
                        if (etr == null)
                                /* FIXME: report error? can be PrimitiveTypeRef or TypeRef */
                                return null;
                                
                        System.Type t = etr.GetReflectedType ();
                        return (t.IsEnum ? Enum.Parse (t, String.Format ("{0}", val)) : val);
                }

		/* Converts a type_spec to a corresponding PermPair */
                PermPair TypeSpecToPermPair (object action, object type_spec, ArrayList pairs)
                {
                        ExternTypeRef etr = type_spec as ExternTypeRef;
                        if (etr == null)
                                /* FIXME: could be PrimitiveTypeRef or TypeRef 
                                          Report what error? */
                                return null;

                        System.Type t = etr.GetReflectedType ();
                        object obj = Activator.CreateInstance (t, 
                                                new object [] {(System.Security.Permissions.SecurityAction) (short) action});

                        if (pairs != null)
                                foreach (NameValuePair pair in pairs) {
                                        PropertyInfo pi = t.GetProperty (pair.Name);
                                        pi.SetValue (obj, pair.Value, null);
                                }

                        IPermission iper = (IPermission) t.GetMethod ("CreatePermission").Invoke (obj, null);
                        return new PermPair ((PEAPI.SecurityAction) action, iper);
                }

		public ILParser (CodeGen codegen, ILTokenizer tokenizer)
                {
			this.codegen = codegen;
                        this.tokenizer = tokenizer;
		}

		public CodeGen CodeGen {
			get { return codegen; }
		}

                private BaseTypeRef GetTypeRef (BaseTypeRef b)
                {
                        //FIXME: Caching required.. 
                        return b.Clone ();
                }

#line default

  /** error output stream.
      It should be changeable.
    */
  public System.IO.TextWriter ErrorOutput = System.Console.Out;

  /** simplified error message.
      @see <a href="#yyerror(java.lang.String, java.lang.String[])">yyerror</a>
    */
  public void yyerror (string message) {
    yyerror(message, null);
  }
#pragma warning disable 649
  /* An EOF token */
  public int eof_token;
#pragma warning restore 649
  /** (syntax) error message.
      Can be overwritten to control message format.
      @param message text to be displayed.
      @param expected vector of acceptable tokens, if available.
    */
  public void yyerror (string message, string[] expected) {
    if ((yacc_verbose_flag > 0) && (expected != null) && (expected.Length  > 0)) {
      ErrorOutput.Write (message+", expecting");
      for (int n = 0; n < expected.Length; ++ n)
        ErrorOutput.Write (" "+expected[n]);
        ErrorOutput.WriteLine ();
    } else
      ErrorOutput.WriteLine (message);
  }

  /** debugging support, requires the package jay.yydebug.
      Set to null to suppress debugging messages.
    */
  internal yydebug.yyDebug debug;

  protected const int yyFinal = 1;
 // Put this array into a separate class so it is only initialized if debugging is actually used
 // Use MarshalByRefObject to disable inlining
 class YYRules : MarshalByRefObject {
  public static readonly string [] yyRule = {
    "$accept : il_file",
    "il_file : decls",
    "decls :",
    "decls : decls decl",
    "decl : class_all",
    "decl : namespace_all",
    "decl : method_all",
    "decl : field_decl",
    "decl : data_decl",
    "decl : vtfixup_decl",
    "decl : file_decl",
    "decl : assembly_all",
    "decl : assemblyref_all",
    "decl : exptype_all",
    "decl : manifestres_all",
    "decl : module_head",
    "decl : sec_decl",
    "decl : customattr_decl",
    "decl : D_SUBSYSTEM int32",
    "decl : D_CORFLAGS int32",
    "decl : D_FILE K_ALIGNMENT int32",
    "decl : D_IMAGEBASE int64",
    "decl : D_STACKRESERVE int64",
    "decl : extsource_spec",
    "decl : language_decl",
    "extsource_spec : D_LINE int32 SQSTRING",
    "extsource_spec : D_LINE int32",
    "extsource_spec : D_LINE int32 COLON int32 SQSTRING",
    "extsource_spec : D_LINE int32 COLON int32",
    "language_decl : D_LANGUAGE SQSTRING",
    "language_decl : D_LANGUAGE SQSTRING COMMA SQSTRING",
    "language_decl : D_LANGUAGE SQSTRING COMMA SQSTRING COMMA SQSTRING",
    "vtfixup_decl : D_VTFIXUP OPEN_BRACKET int32 CLOSE_BRACKET vtfixup_attr K_AT id",
    "vtfixup_attr :",
    "vtfixup_attr : vtfixup_attr K_INT32",
    "vtfixup_attr : vtfixup_attr K_INT64",
    "vtfixup_attr : vtfixup_attr K_FROMUNMANAGED",
    "vtfixup_attr : vtfixup_attr K_CALLMOSTDERIVED",
    "namespace_all : namespace_head OPEN_BRACE decls CLOSE_BRACE",
    "namespace_head : D_NAMESPACE comp_name",
    "class_all : class_head OPEN_BRACE class_decls CLOSE_BRACE",
    "class_head : D_CLASS class_attr comp_name formal_typars_clause extends_clause impl_clause",
    "class_attr :",
    "class_attr : class_attr K_PUBLIC",
    "class_attr : class_attr K_PRIVATE",
    "class_attr : class_attr K_NESTED K_PRIVATE",
    "class_attr : class_attr K_NESTED K_PUBLIC",
    "class_attr : class_attr K_NESTED K_FAMILY",
    "class_attr : class_attr K_NESTED K_ASSEMBLY",
    "class_attr : class_attr K_NESTED K_FAMANDASSEM",
    "class_attr : class_attr K_NESTED K_FAMORASSEM",
    "class_attr : class_attr K_VALUE",
    "class_attr : class_attr K_ENUM",
    "class_attr : class_attr K_INTERFACE",
    "class_attr : class_attr K_SEALED",
    "class_attr : class_attr K_ABSTRACT",
    "class_attr : class_attr K_AUTO",
    "class_attr : class_attr K_SEQUENTIAL",
    "class_attr : class_attr K_EXPLICIT",
    "class_attr : class_attr K_ANSI",
    "class_attr : class_attr K_UNICODE",
    "class_attr : class_attr K_AUTOCHAR",
    "class_attr : class_attr K_IMPORT",
    "class_attr : class_attr K_SERIALIZABLE",
    "class_attr : class_attr K_BEFOREFIELDINIT",
    "class_attr : class_attr K_SPECIALNAME",
    "class_attr : class_attr K_RTSPECIALNAME",
    "extends_clause :",
    "extends_clause : K_EXTENDS generic_class_ref",
    "impl_clause :",
    "impl_clause : impl_class_refs",
    "impl_class_refs : K_IMPLEMENTS generic_class_ref",
    "impl_class_refs : impl_class_refs COMMA generic_class_ref",
    "formal_typars_clause :",
    "formal_typars_clause : OPEN_ANGLE_BRACKET formal_typars CLOSE_ANGLE_BRACKET",
    "typars_clause :",
    "typars_clause : OPEN_ANGLE_BRACKET typars CLOSE_ANGLE_BRACKET",
    "typars : type",
    "typars : typars COMMA type",
    "constraints_clause :",
    "constraints_clause : OPEN_PARENS constraints CLOSE_PARENS",
    "constraints : type",
    "constraints : constraints COMMA type",
    "generic_class_ref : class_ref",
    "generic_class_ref : K_OBJECT",
    "generic_class_ref : K_CLASS class_ref typars_clause",
    "generic_class_ref : BANG int32",
    "generic_class_ref : BANG BANG int32",
    "generic_class_ref : BANG id",
    "generic_class_ref : BANG BANG id",
    "formal_typars : formal_typar_attr constraints_clause formal_typar",
    "formal_typars : formal_typars COMMA formal_typar_attr constraints_clause formal_typar",
    "formal_typar_attr :",
    "formal_typar_attr : formal_typar_attr PLUS",
    "formal_typar_attr : formal_typar_attr DASH",
    "formal_typar_attr : formal_typar_attr D_CTOR",
    "formal_typar_attr : formal_typar_attr K_VALUETYPE",
    "formal_typar_attr : formal_typar_attr K_CLASS",
    "formal_typar : id",
    "param_type_decl : D_PARAM K_TYPE id",
    "param_type_decl : D_PARAM K_TYPE OPEN_BRACKET int32 CLOSE_BRACKET",
    "class_refs : class_ref",
    "class_refs : class_refs COMMA class_ref",
    "slashed_name : comp_name",
    "slashed_name : slashed_name SLASH comp_name",
    "class_ref : OPEN_BRACKET slashed_name CLOSE_BRACKET slashed_name",
    "class_ref : OPEN_BRACKET D_MODULE slashed_name CLOSE_BRACKET slashed_name",
    "class_ref : slashed_name",
    "class_decls :",
    "class_decls : class_decls class_decl",
    "class_decl : method_all",
    "class_decl : class_all",
    "class_decl : event_all",
    "class_decl : prop_all",
    "class_decl : field_decl",
    "class_decl : data_decl",
    "class_decl : sec_decl",
    "class_decl : extsource_spec",
    "class_decl : customattr_decl",
    "class_decl : param_type_decl",
    "class_decl : D_SIZE int32",
    "class_decl : D_PACK int32",
    "$$1 :",
    "class_decl : D_OVERRIDE type_spec DOUBLE_COLON method_name K_WITH call_conv type type_spec DOUBLE_COLON method_name type_list $$1 OPEN_PARENS sig_args CLOSE_PARENS",
    "class_decl : language_decl",
    "type : generic_class_ref",
    "type : K_VALUE K_CLASS class_ref",
    "type : K_VALUETYPE OPEN_BRACKET slashed_name CLOSE_BRACKET slashed_name typars_clause",
    "type : K_VALUETYPE slashed_name typars_clause",
    "type : type OPEN_BRACKET CLOSE_BRACKET",
    "type : type OPEN_BRACKET bounds CLOSE_BRACKET",
    "type : type AMPERSAND",
    "type : type STAR",
    "type : type K_PINNED",
    "type : type K_MODREQ OPEN_PARENS custom_modifier_type CLOSE_PARENS",
    "type : type K_MODOPT OPEN_PARENS custom_modifier_type CLOSE_PARENS",
    "type : K_METHOD call_conv type STAR OPEN_PARENS sig_args CLOSE_PARENS",
    "type : primitive_type",
    "primitive_type : K_INT8",
    "primitive_type : K_INT16",
    "primitive_type : K_INT32",
    "primitive_type : K_INT64",
    "primitive_type : K_FLOAT32",
    "primitive_type : K_FLOAT64",
    "primitive_type : K_UNSIGNED K_INT8",
    "primitive_type : K_UINT8",
    "primitive_type : K_UNSIGNED K_INT16",
    "primitive_type : K_UINT16",
    "primitive_type : K_UNSIGNED K_INT32",
    "primitive_type : K_UINT32",
    "primitive_type : K_UNSIGNED K_INT64",
    "primitive_type : K_UINT64",
    "primitive_type : K_NATIVE K_INT",
    "primitive_type : K_NATIVE K_UNSIGNED K_INT",
    "primitive_type : K_NATIVE K_UINT",
    "primitive_type : K_TYPEDREF",
    "primitive_type : K_CHAR",
    "primitive_type : K_WCHAR",
    "primitive_type : K_VOID",
    "primitive_type : K_BOOL",
    "primitive_type : K_STRING",
    "bounds : bound",
    "bounds : bounds COMMA bound",
    "bound :",
    "bound : ELLIPSIS",
    "bound : int32",
    "bound : int32 ELLIPSIS int32",
    "bound : int32 ELLIPSIS",
    "call_conv : K_INSTANCE call_conv",
    "call_conv : K_EXPLICIT call_conv",
    "call_conv : call_kind",
    "call_kind :",
    "call_kind : K_DEFAULT",
    "call_kind : K_VARARG",
    "call_kind : K_UNMANAGED K_CDECL",
    "call_kind : K_UNMANAGED K_STDCALL",
    "call_kind : K_UNMANAGED K_THISCALL",
    "call_kind : K_UNMANAGED K_FASTCALL",
    "native_type :",
    "native_type : K_CUSTOM OPEN_PARENS comp_qstring COMMA comp_qstring CLOSE_PARENS",
    "native_type : K_FIXED K_SYSSTRING OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : K_FIXED K_ARRAY OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : K_VARIANT",
    "native_type : K_CURRENCY",
    "native_type : K_SYSCHAR",
    "native_type : K_VOID",
    "native_type : K_BOOL",
    "native_type : K_INT8",
    "native_type : K_INT16",
    "native_type : K_INT32",
    "native_type : K_INT64",
    "native_type : K_FLOAT32",
    "native_type : K_FLOAT64",
    "native_type : K_ERROR",
    "native_type : K_UNSIGNED K_INT8",
    "native_type : K_UINT8",
    "native_type : K_UNSIGNED K_INT16",
    "native_type : K_UINT16",
    "native_type : K_UNSIGNED K_INT32",
    "native_type : K_UINT32",
    "native_type : K_UNSIGNED K_INT64",
    "native_type : K_UINT64",
    "native_type : native_type STAR",
    "native_type : native_type OPEN_BRACKET CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET int32 CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET int32 PLUS int32 CLOSE_BRACKET",
    "native_type : native_type OPEN_BRACKET PLUS int32 CLOSE_BRACKET",
    "native_type : K_DECIMAL",
    "native_type : K_DATE",
    "native_type : K_BSTR",
    "native_type : K_LPSTR",
    "native_type : K_LPWSTR",
    "native_type : K_LPTSTR",
    "native_type : K_OBJECTREF",
    "native_type : K_IUNKNOWN",
    "native_type : K_IDISPATCH",
    "native_type : K_STRUCT",
    "native_type : K_INTERFACE",
    "native_type : K_SAFEARRAY variant_type",
    "native_type : K_SAFEARRAY variant_type COMMA comp_qstring",
    "native_type : K_INT",
    "native_type : K_UNSIGNED K_INT",
    "native_type : K_NESTED K_STRUCT",
    "native_type : K_BYVALSTR",
    "native_type : K_ANSI K_BSTR",
    "native_type : K_TBSTR",
    "native_type : K_VARIANT K_BOOL",
    "native_type : K_METHOD",
    "native_type : K_AS K_ANY",
    "native_type : K_LPSTRUCT",
    "variant_type :",
    "variant_type : K_NULL",
    "variant_type : K_VARIANT",
    "variant_type : K_CURRENCY",
    "variant_type : K_VOID",
    "variant_type : K_BOOL",
    "variant_type : K_INT8",
    "variant_type : K_INT16",
    "variant_type : K_INT32",
    "variant_type : K_INT64",
    "variant_type : K_FLOAT32",
    "variant_type : K_FLOAT64",
    "variant_type : K_UNSIGNED K_INT8",
    "variant_type : K_UNSIGNED K_INT16",
    "variant_type : K_UNSIGNED K_INT32",
    "variant_type : K_UNSIGNED K_INT64",
    "variant_type : STAR",
    "variant_type : variant_type OPEN_BRACKET CLOSE_BRACKET",
    "variant_type : variant_type K_VECTOR",
    "variant_type : variant_type AMPERSAND",
    "variant_type : K_DECIMAL",
    "variant_type : K_DATE",
    "variant_type : K_BSTR",
    "variant_type : K_LPSTR",
    "variant_type : K_LPWSTR",
    "variant_type : K_IUNKNOWN",
    "variant_type : K_IDISPATCH",
    "variant_type : K_SAFEARRAY",
    "variant_type : K_INT",
    "variant_type : K_UNSIGNED K_INT",
    "variant_type : K_ERROR",
    "variant_type : K_HRESULT",
    "variant_type : K_CARRAY",
    "variant_type : K_USERDEFINED",
    "variant_type : K_RECORD",
    "variant_type : K_FILETIME",
    "variant_type : K_BLOB",
    "variant_type : K_STREAM",
    "variant_type : K_STORAGE",
    "variant_type : K_STREAMED_OBJECT",
    "variant_type : K_STORED_OBJECT",
    "variant_type : K_BLOB_OBJECT",
    "variant_type : K_CF",
    "variant_type : K_CLSID",
    "custom_modifier_type : primitive_type",
    "custom_modifier_type : class_ref",
    "field_decl : D_FIELD repeat_opt field_attr type id at_opt init_opt semicolon_opt",
    "repeat_opt :",
    "repeat_opt : OPEN_BRACKET int32 CLOSE_BRACKET",
    "field_attr :",
    "field_attr : field_attr K_PUBLIC",
    "field_attr : field_attr K_PRIVATE",
    "field_attr : field_attr K_FAMILY",
    "field_attr : field_attr K_ASSEMBLY",
    "field_attr : field_attr K_FAMANDASSEM",
    "field_attr : field_attr K_FAMORASSEM",
    "field_attr : field_attr K_PRIVATESCOPE",
    "field_attr : field_attr K_STATIC",
    "field_attr : field_attr K_INITONLY",
    "field_attr : field_attr K_RTSPECIALNAME",
    "field_attr : field_attr K_SPECIALNAME",
    "field_attr : field_attr K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS",
    "field_attr : field_attr K_LITERAL",
    "field_attr : field_attr K_NOTSERIALIZED",
    "at_opt :",
    "at_opt : K_AT id",
    "init_opt :",
    "init_opt : ASSIGN field_init",
    "field_init_primitive : K_FLOAT32 OPEN_PARENS float64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT64 OPEN_PARENS float64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_FLOAT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT64 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT32 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT16 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT16 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_CHAR OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_WCHAR OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_INT8 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_UINT8 OPEN_PARENS int64 CLOSE_PARENS",
    "field_init_primitive : K_BOOL OPEN_PARENS truefalse CLOSE_PARENS",
    "field_init_full : field_init_primitive",
    "field_init_full : K_BYTEARRAY bytes_list",
    "field_init : field_init_full",
    "field_init : comp_qstring",
    "field_init : K_NULLREF",
    "member_init : field_init_full",
    "member_init : K_STRING OPEN_PARENS SQSTRING CLOSE_PARENS",
    "opt_truefalse_list : truefalse_list",
    "truefalse_list : truefalse",
    "truefalse_list : truefalse_list truefalse",
    "data_decl : data_head data_body",
    "data_head : D_DATA tls id ASSIGN",
    "data_head : D_DATA tls",
    "tls :",
    "tls : K_TLS",
    "data_body : OPEN_BRACE dataitem_list CLOSE_BRACE",
    "data_body : dataitem",
    "dataitem_list : dataitem",
    "dataitem_list : dataitem_list COMMA dataitem",
    "dataitem : K_CHAR STAR OPEN_PARENS comp_qstring CLOSE_PARENS",
    "dataitem : K_WCHAR STAR OPEN_PARENS comp_qstring CLOSE_PARENS",
    "dataitem : AMPERSAND OPEN_PARENS id CLOSE_PARENS",
    "dataitem : K_BYTEARRAY ASSIGN bytes_list",
    "dataitem : K_BYTEARRAY bytes_list",
    "dataitem : K_FLOAT32 OPEN_PARENS float64 CLOSE_PARENS repeat_opt",
    "dataitem : K_FLOAT64 OPEN_PARENS float64 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT64 OPEN_PARENS int64 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT32 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT16 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_INT8 OPEN_PARENS int32 CLOSE_PARENS repeat_opt",
    "dataitem : K_FLOAT32 repeat_opt",
    "dataitem : K_FLOAT64 repeat_opt",
    "dataitem : K_INT64 repeat_opt",
    "dataitem : K_INT32 repeat_opt",
    "dataitem : K_INT16 repeat_opt",
    "dataitem : K_INT8 repeat_opt",
    "method_all : method_head OPEN_BRACE method_decls CLOSE_BRACE",
    "method_head : D_METHOD meth_attr call_conv param_attr type method_name formal_typars_clause OPEN_PARENS sig_args CLOSE_PARENS impl_attr",
    "method_head : D_METHOD meth_attr call_conv param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS method_name OPEN_PARENS sig_args CLOSE_PARENS impl_attr",
    "meth_attr :",
    "meth_attr : meth_attr K_STATIC",
    "meth_attr : meth_attr K_PUBLIC",
    "meth_attr : meth_attr K_PRIVATE",
    "meth_attr : meth_attr K_FAMILY",
    "meth_attr : meth_attr K_ASSEMBLY",
    "meth_attr : meth_attr K_FAMANDASSEM",
    "meth_attr : meth_attr K_FAMORASSEM",
    "meth_attr : meth_attr K_PRIVATESCOPE",
    "meth_attr : meth_attr K_FINAL",
    "meth_attr : meth_attr K_VIRTUAL",
    "meth_attr : meth_attr K_ABSTRACT",
    "meth_attr : meth_attr K_HIDEBYSIG",
    "meth_attr : meth_attr K_NEWSLOT",
    "meth_attr : meth_attr K_REQSECOBJ",
    "meth_attr : meth_attr K_SPECIALNAME",
    "meth_attr : meth_attr K_RTSPECIALNAME",
    "meth_attr : meth_attr K_STRICT",
    "meth_attr : meth_attr K_COMPILERCONTROLLED",
    "meth_attr : meth_attr K_UNMANAGEDEXP",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS comp_qstring K_AS comp_qstring pinv_attr CLOSE_PARENS",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS comp_qstring pinv_attr CLOSE_PARENS",
    "meth_attr : meth_attr K_PINVOKEIMPL OPEN_PARENS pinv_attr CLOSE_PARENS",
    "pinv_attr :",
    "pinv_attr : pinv_attr K_NOMANGLE",
    "pinv_attr : pinv_attr K_ANSI",
    "pinv_attr : pinv_attr K_UNICODE",
    "pinv_attr : pinv_attr K_AUTOCHAR",
    "pinv_attr : pinv_attr K_LASTERR",
    "pinv_attr : pinv_attr K_WINAPI",
    "pinv_attr : pinv_attr K_CDECL",
    "pinv_attr : pinv_attr K_STDCALL",
    "pinv_attr : pinv_attr K_THISCALL",
    "pinv_attr : pinv_attr K_FASTCALL",
    "pinv_attr : pinv_attr K_BESTFIT COLON K_ON",
    "pinv_attr : pinv_attr K_BESTFIT COLON K_OFF",
    "pinv_attr : pinv_attr K_CHARMAPERROR COLON K_ON",
    "pinv_attr : pinv_attr K_CHARMAPERROR COLON K_OFF",
    "method_name : D_CTOR",
    "method_name : D_CCTOR",
    "method_name : comp_name",
    "param_attr :",
    "param_attr : param_attr OPEN_BRACKET K_IN CLOSE_BRACKET",
    "param_attr : param_attr OPEN_BRACKET K_OUT CLOSE_BRACKET",
    "param_attr : param_attr OPEN_BRACKET K_OPT CLOSE_BRACKET",
    "impl_attr :",
    "impl_attr : impl_attr K_NATIVE",
    "impl_attr : impl_attr K_CIL",
    "impl_attr : impl_attr K_IL",
    "impl_attr : impl_attr K_OPTIL",
    "impl_attr : impl_attr K_MANAGED",
    "impl_attr : impl_attr K_UNMANAGED",
    "impl_attr : impl_attr K_FORWARDREF",
    "impl_attr : impl_attr K_PRESERVESIG",
    "impl_attr : impl_attr K_RUNTIME",
    "impl_attr : impl_attr K_INTERNALCALL",
    "impl_attr : impl_attr K_SYNCHRONIZED",
    "impl_attr : impl_attr K_NOINLINING",
    "sig_args :",
    "sig_args : sig_arg_list",
    "sig_arg_list : sig_arg",
    "sig_arg_list : sig_arg_list COMMA sig_arg",
    "sig_arg : param_attr type",
    "sig_arg : param_attr type id",
    "sig_arg : ELLIPSIS",
    "sig_arg : param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS",
    "sig_arg : param_attr type K_MARSHAL OPEN_PARENS native_type CLOSE_PARENS id",
    "type_list :",
    "type_list : ELLIPSIS",
    "type_list : type_list COMMA ELLIPSIS",
    "type_list : param_attr type opt_id",
    "type_list : type_list COMMA param_attr type opt_id",
    "opt_id :",
    "opt_id : id",
    "method_decls :",
    "method_decls : method_decls method_decl",
    "method_decl : D_EMITBYTE int32",
    "method_decl : D_MAXSTACK int32",
    "method_decl : D_LOCALS OPEN_PARENS local_list CLOSE_PARENS",
    "method_decl : D_LOCALS K_INIT OPEN_PARENS local_list CLOSE_PARENS",
    "method_decl : D_ENTRYPOINT",
    "method_decl : D_ZEROINIT",
    "method_decl : D_EXPORT OPEN_BRACKET int32 CLOSE_BRACKET",
    "method_decl : D_EXPORT OPEN_BRACKET int32 CLOSE_BRACKET K_AS id",
    "method_decl : D_VTENTRY int32 COLON int32",
    "method_decl : D_OVERRIDE type_spec DOUBLE_COLON method_name",
    "method_decl : D_OVERRIDE K_METHOD method_ref",
    "method_decl : D_OVERRIDE K_METHOD call_conv type type_spec DOUBLE_COLON method_name OPEN_ANGLE_BRACKET OPEN_BRACKET int32 CLOSE_BRACKET CLOSE_ANGLE_BRACKET OPEN_PARENS type_list CLOSE_PARENS",
    "method_decl : scope_block",
    "method_decl : D_PARAM OPEN_BRACKET int32 CLOSE_BRACKET init_opt",
    "method_decl : param_type_decl",
    "method_decl : id COLON",
    "method_decl : seh_block",
    "method_decl : instr",
    "method_decl : sec_decl",
    "method_decl : extsource_spec",
    "method_decl : language_decl",
    "method_decl : customattr_decl",
    "method_decl : data_decl",
    "local_list :",
    "local_list : local",
    "local_list : local_list COMMA local",
    "local : type",
    "local : type id",
    "local : slot_num type",
    "local : slot_num type id",
    "slot_num : OPEN_BRACKET int32 CLOSE_BRACKET",
    "type_spec : OPEN_BRACKET slashed_name CLOSE_BRACKET",
    "type_spec : OPEN_BRACKET D_MODULE slashed_name CLOSE_BRACKET",
    "type_spec : type",
    "scope_block : scope_block_begin method_decls CLOSE_BRACE",
    "scope_block_begin : OPEN_BRACE",
    "seh_block : try_block seh_clauses",
    "try_block : D_TRY scope_block",
    "try_block : D_TRY id K_TO id",
    "try_block : D_TRY int32 K_TO int32",
    "seh_clauses : seh_clause",
    "seh_clauses : seh_clauses seh_clause",
    "seh_clause : K_CATCH type handler_block",
    "seh_clause : K_FINALLY handler_block",
    "seh_clause : K_FAULT handler_block",
    "seh_clause : filter_clause handler_block",
    "filter_clause : K_FILTER scope_block",
    "filter_clause : K_FILTER id",
    "filter_clause : K_FILTER int32",
    "handler_block : scope_block",
    "handler_block : K_HANDLER id K_TO id",
    "handler_block : K_HANDLER int32 K_TO int32",
    "instr : INSTR_NONE",
    "instr : INSTR_LOCAL int32",
    "instr : INSTR_LOCAL id",
    "instr : INSTR_PARAM int32",
    "instr : INSTR_PARAM id",
    "instr : INSTR_I int32",
    "instr : INSTR_I id",
    "instr : INSTR_I8 int64",
    "instr : INSTR_R float64",
    "instr : INSTR_R int64",
    "instr : INSTR_R bytes_list",
    "instr : INSTR_BRTARGET int32",
    "instr : INSTR_BRTARGET id",
    "instr : INSTR_METHOD method_ref",
    "instr : INSTR_FIELD type type_spec DOUBLE_COLON id",
    "instr : INSTR_FIELD type id",
    "instr : INSTR_TYPE type_spec",
    "instr : INSTR_STRING comp_qstring",
    "instr : INSTR_STRING K_BYTEARRAY ASSIGN bytes_list",
    "instr : INSTR_STRING K_BYTEARRAY bytes_list",
    "instr : INSTR_SIG call_conv type OPEN_PARENS type_list CLOSE_PARENS",
    "instr : INSTR_TOK owner_type",
    "instr : INSTR_SWITCH OPEN_PARENS labels CLOSE_PARENS",
    "method_ref : call_conv type method_name typars_clause OPEN_PARENS type_list CLOSE_PARENS",
    "method_ref : call_conv type type_spec DOUBLE_COLON method_name typars_clause OPEN_PARENS type_list CLOSE_PARENS",
    "labels :",
    "labels : id",
    "labels : int32",
    "labels : labels COMMA id",
    "labels : labels COMMA int32",
    "owner_type : type_spec",
    "owner_type : member_ref",
    "member_ref : K_METHOD method_ref",
    "member_ref : K_FIELD type type_spec DOUBLE_COLON id",
    "member_ref : K_FIELD type id",
    "event_all : event_head OPEN_BRACE event_decls CLOSE_BRACE",
    "event_head : D_EVENT event_attr type_spec comp_name",
    "event_head : D_EVENT event_attr id",
    "event_attr :",
    "event_attr : event_attr K_RTSPECIALNAME",
    "event_attr : event_attr K_SPECIALNAME",
    "event_decls :",
    "event_decls : event_decls event_decl",
    "event_decl : D_ADDON method_ref semicolon_opt",
    "event_decl : D_REMOVEON method_ref semicolon_opt",
    "event_decl : D_FIRE method_ref semicolon_opt",
    "event_decl : D_OTHER method_ref semicolon_opt",
    "event_decl : customattr_decl",
    "event_decl : extsource_spec",
    "event_decl : language_decl",
    "prop_all : prop_head OPEN_BRACE prop_decls CLOSE_BRACE",
    "prop_head : D_PROPERTY prop_attr type comp_name OPEN_PARENS type_list CLOSE_PARENS init_opt",
    "prop_attr :",
    "prop_attr : prop_attr K_RTSPECIALNAME",
    "prop_attr : prop_attr K_SPECIALNAME",
    "prop_attr : prop_attr K_INSTANCE",
    "prop_decls :",
    "prop_decls : prop_decls prop_decl",
    "prop_decl : D_SET method_ref",
    "prop_decl : D_GET method_ref",
    "prop_decl : D_OTHER method_ref",
    "prop_decl : customattr_decl",
    "prop_decl : extsource_spec",
    "prop_decl : language_decl",
    "customattr_decl : D_CUSTOM customattr_owner_type_opt custom_type",
    "customattr_decl : D_CUSTOM customattr_owner_type_opt custom_type ASSIGN comp_qstring",
    "customattr_decl : D_CUSTOM customattr_owner_type_opt custom_type ASSIGN bytes_list",
    "customattr_decl : D_CUSTOM customattr_owner_type_opt custom_type ASSIGN OPEN_BRACE customattr_values CLOSE_BRACE",
    "customattr_owner_type_opt :",
    "customattr_owner_type_opt : OPEN_PARENS type CLOSE_PARENS",
    "customattr_values :",
    "customattr_values : K_BOOL OPEN_BRACKET int32 CLOSE_BRACKET OPEN_PARENS opt_truefalse_list CLOSE_PARENS",
    "customattr_values : K_BYTEARRAY bytes_list",
    "customattr_values : K_STRING OPEN_PARENS SQSTRING CLOSE_PARENS",
    "customattr_values : customattr_ctor_args",
    "customattr_ctor_args : customattr_ctor_arg",
    "customattr_ctor_args : customattr_ctor_args customattr_ctor_arg",
    "customattr_ctor_arg : field_init_primitive",
    "customattr_ctor_arg : K_TYPE OPEN_PARENS type CLOSE_PARENS",
    "custom_type : call_conv type type_spec DOUBLE_COLON method_name OPEN_PARENS type_list CLOSE_PARENS",
    "custom_type : call_conv type method_name OPEN_PARENS type_list CLOSE_PARENS",
    "sec_decl : D_PERMISSION sec_action type_spec OPEN_PARENS nameval_pairs CLOSE_PARENS",
    "sec_decl : D_PERMISSION sec_action type_spec",
    "sec_decl : D_PERMISSIONSET sec_action ASSIGN bytes_list",
    "sec_decl : D_PERMISSIONSET sec_action comp_qstring",
    "sec_decl : D_PERMISSIONSET sec_action ASSIGN OPEN_BRACE permissions CLOSE_BRACE",
    "permissions : permission",
    "permissions : permissions COMMA permission",
    "permission : class_ref ASSIGN OPEN_BRACE permission_members CLOSE_BRACE",
    "permission_members : permission_member",
    "permission_members : permission_members permission_member",
    "permission_member : prop_or_field primitive_type perm_mbr_nameval_pair",
    "permission_member : prop_or_field K_ENUM class_ref perm_mbr_nameval_pair",
    "perm_mbr_nameval_pair : SQSTRING ASSIGN member_init",
    "prop_or_field : K_PROPERTY",
    "prop_or_field : K_FIELD",
    "nameval_pairs : nameval_pair",
    "nameval_pairs : nameval_pairs COMMA nameval_pair",
    "nameval_pair : comp_qstring ASSIGN cavalue",
    "cavalue : truefalse",
    "cavalue : int32",
    "cavalue : int32 OPEN_PARENS int32 CLOSE_PARENS",
    "cavalue : comp_qstring",
    "cavalue : class_ref OPEN_PARENS K_INT8 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS K_INT16 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS K_INT32 COLON int32 CLOSE_PARENS",
    "cavalue : class_ref OPEN_PARENS int32 CLOSE_PARENS",
    "sec_action : K_REQUEST",
    "sec_action : K_DEMAND",
    "sec_action : K_ASSERT",
    "sec_action : K_DENY",
    "sec_action : K_PERMITONLY",
    "sec_action : K_LINKCHECK",
    "sec_action : K_INHERITCHECK",
    "sec_action : K_REQMIN",
    "sec_action : K_REQOPT",
    "sec_action : K_REQREFUSE",
    "sec_action : K_PREJITGRANT",
    "sec_action : K_PREJITDENY",
    "sec_action : K_NONCASDEMAND",
    "sec_action : K_NONCASLINKDEMAND",
    "sec_action : K_NONCASINHERITANCE",
    "module_head : D_MODULE",
    "module_head : D_MODULE comp_name",
    "module_head : D_MODULE K_EXTERN comp_name",
    "file_decl : D_FILE file_attr comp_name file_entry D_HASH ASSIGN bytes_list file_entry",
    "file_decl : D_FILE file_attr comp_name file_entry",
    "file_attr :",
    "file_attr : file_attr K_NOMETADATA",
    "file_entry :",
    "file_entry : D_ENTRYPOINT",
    "assembly_all : assembly_head OPEN_BRACE assembly_decls CLOSE_BRACE",
    "assembly_head : D_ASSEMBLY legacylibrary_opt asm_attr slashed_name",
    "asm_attr :",
    "asm_attr : asm_attr K_RETARGETABLE",
    "assembly_decls :",
    "assembly_decls : assembly_decls assembly_decl",
    "assembly_decl : D_PUBLICKEY ASSIGN bytes_list",
    "assembly_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "assembly_decl : D_LOCALE comp_qstring",
    "assembly_decl : D_LOCALE ASSIGN bytes_list",
    "assembly_decl : D_HASH K_ALGORITHM int32",
    "assembly_decl : customattr_decl",
    "assembly_decl : sec_decl",
    "asm_or_ref_decl : D_PUBLICKEY ASSIGN bytes_list",
    "asm_or_ref_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "asm_or_ref_decl : D_LOCALE comp_qstring",
    "asm_or_ref_decl : D_LOCALE ASSIGN bytes_list",
    "asm_or_ref_decl : customattr_decl",
    "assemblyref_all : assemblyref_head OPEN_BRACE assemblyref_decls CLOSE_BRACE",
    "assemblyref_head : D_ASSEMBLY K_EXTERN legacylibrary_opt asm_attr slashed_name",
    "assemblyref_head : D_ASSEMBLY K_EXTERN legacylibrary_opt asm_attr slashed_name K_AS slashed_name",
    "assemblyref_decls :",
    "assemblyref_decls : assemblyref_decls assemblyref_decl",
    "assemblyref_decl : D_VER int32 COLON int32 COLON int32 COLON int32",
    "assemblyref_decl : D_PUBLICKEY ASSIGN bytes_list",
    "assemblyref_decl : D_PUBLICKEYTOKEN ASSIGN bytes_list",
    "assemblyref_decl : D_LOCALE comp_qstring",
    "assemblyref_decl : D_LOCALE ASSIGN bytes_list",
    "assemblyref_decl : D_HASH ASSIGN bytes_list",
    "assemblyref_decl : customattr_decl",
    "assemblyref_decl : K_AUTO",
    "exptype_all : exptype_head OPEN_BRACE exptype_decls CLOSE_BRACE",
    "exptype_head : D_CLASS K_EXTERN expt_attr comp_name",
    "expt_attr :",
    "expt_attr : expt_attr K_PRIVATE",
    "expt_attr : expt_attr K_PUBLIC",
    "expt_attr : expt_attr K_FORWARDER",
    "expt_attr : expt_attr K_NESTED K_PUBLIC",
    "expt_attr : expt_attr K_NESTED K_PRIVATE",
    "expt_attr : expt_attr K_NESTED K_FAMILY",
    "expt_attr : expt_attr K_NESTED K_ASSEMBLY",
    "expt_attr : expt_attr K_NESTED K_FAMANDASSEM",
    "expt_attr : expt_attr K_NESTED K_FAMORASSEM",
    "exptype_decls :",
    "exptype_decls : exptype_decls exptype_decl",
    "exptype_decl : D_FILE comp_name",
    "exptype_decl : D_CLASS K_EXTERN comp_name",
    "exptype_decl : customattr_decl",
    "exptype_decl : D_ASSEMBLY K_EXTERN comp_name",
    "manifestres_all : manifestres_head OPEN_BRACE manifestres_decls CLOSE_BRACE",
    "manifestres_head : D_MRESOURCE manres_attr comp_name",
    "manres_attr :",
    "manres_attr : manres_attr K_PUBLIC",
    "manres_attr : manres_attr K_PRIVATE",
    "manifestres_decls :",
    "manifestres_decls : manifestres_decls manifestres_decl",
    "manifestres_decl : D_FILE comp_name K_AT int32",
    "manifestres_decl : D_ASSEMBLY K_EXTERN slashed_name",
    "manifestres_decl : customattr_decl",
    "comp_qstring : QSTRING",
    "comp_qstring : comp_qstring PLUS QSTRING",
    "int32 : INT64",
    "int64 : INT64",
    "float64 : FLOAT64",
    "float64 : K_FLOAT32 OPEN_PARENS INT32 CLOSE_PARENS",
    "float64 : K_FLOAT32 OPEN_PARENS INT64 CLOSE_PARENS",
    "float64 : K_FLOAT64 OPEN_PARENS INT64 CLOSE_PARENS",
    "float64 : K_FLOAT64 OPEN_PARENS INT32 CLOSE_PARENS",
    "hexbyte : HEXBYTE",
    "$$2 :",
    "bytes_list : OPEN_PARENS $$2 bytes CLOSE_PARENS",
    "bytes :",
    "bytes : hexbytes",
    "hexbytes : hexbyte",
    "hexbytes : hexbytes hexbyte",
    "truefalse : K_TRUE",
    "truefalse : K_FALSE",
    "id : ID",
    "id : SQSTRING",
    "comp_name : id",
    "comp_name : comp_name DOT comp_name",
    "comp_name : COMP_NAME",
    "semicolon_opt :",
    "semicolon_opt : SEMICOLON",
    "legacylibrary_opt :",
    "legacylibrary_opt : K_LEGACY K_LIBRARY",
  };
 public static string getRule (int index) {
    return yyRule [index];
 }
}
  protected static readonly string [] yyNames = {    
    "end-of-file",null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,"'!'",null,null,null,null,"'&'",
    null,"'('","')'","'*'","'+'","','","'-'","'.'","'/'",null,null,null,
    null,null,null,null,null,null,null,"':'","';'","'<'","'='","'>'",null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,
    "'['",null,"']'",null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,"'{'",null,"'}'",null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    null,null,null,null,null,null,null,null,null,null,null,null,null,null,
    "EOF","ID","QSTRING","SQSTRING","COMP_NAME","INT32","INT64","FLOAT64",
    "HEXBYTE","DOT","OPEN_BRACE","CLOSE_BRACE","OPEN_BRACKET",
    "CLOSE_BRACKET","OPEN_PARENS","CLOSE_PARENS","COMMA","COLON",
    "DOUBLE_COLON","\"::\"","SEMICOLON","ASSIGN","STAR","AMPERSAND",
    "PLUS","SLASH","BANG","ELLIPSIS","\"...\"","DASH",
    "OPEN_ANGLE_BRACKET","CLOSE_ANGLE_BRACKET","UNKNOWN","INSTR_NONE",
    "INSTR_VAR","INSTR_I","INSTR_I8","INSTR_R","INSTR_BRTARGET",
    "INSTR_METHOD","INSTR_NEWOBJ","INSTR_FIELD","INSTR_TYPE",
    "INSTR_STRING","INSTR_SIG","INSTR_RVA","INSTR_TOK","INSTR_SWITCH",
    "INSTR_PHI","INSTR_LOCAL","INSTR_PARAM","D_ADDON","D_ALGORITHM",
    "D_ASSEMBLY","D_BACKING","D_BLOB","D_CAPABILITY","D_CCTOR","D_CLASS",
    "D_COMTYPE","D_CONFIG","D_IMAGEBASE","D_CORFLAGS","D_CTOR","D_CUSTOM",
    "D_DATA","D_EMITBYTE","D_ENTRYPOINT","D_EVENT","D_EXELOC","D_EXPORT",
    "D_FIELD","D_FILE","D_FIRE","D_GET","D_HASH","D_IMPLICITCOM",
    "D_LANGUAGE","D_LINE","D_XLINE","D_LOCALE","D_LOCALS","D_MANIFESTRES",
    "D_MAXSTACK","D_METHOD","D_MIME","D_MODULE","D_MRESOURCE",
    "D_NAMESPACE","D_ORIGINATOR","D_OS","D_OTHER","D_OVERRIDE","D_PACK",
    "D_PARAM","D_PERMISSION","D_PERMISSIONSET","D_PROCESSOR","D_PROPERTY",
    "D_PUBLICKEY","D_PUBLICKEYTOKEN","D_REMOVEON","D_SET","D_SIZE",
    "D_STACKRESERVE","D_SUBSYSTEM","D_TITLE","D_TRY","D_VER","D_VTABLE",
    "D_VTENTRY","D_VTFIXUP","D_ZEROINIT","K_AT","K_AS","K_IMPLICITCOM",
    "K_IMPLICITRES","K_NOAPPDOMAIN","K_NOPROCESS","K_NOMACHINE",
    "K_EXTERN","K_INSTANCE","K_EXPLICIT","K_DEFAULT","K_VARARG",
    "K_UNMANAGED","K_CDECL","K_STDCALL","K_THISCALL","K_FASTCALL",
    "K_MARSHAL","K_IN","K_OUT","K_OPT","K_STATIC","K_PUBLIC","K_PRIVATE",
    "K_FAMILY","K_INITONLY","K_RTSPECIALNAME","K_STRICT","K_SPECIALNAME",
    "K_ASSEMBLY","K_FAMANDASSEM","K_FAMORASSEM","K_PRIVATESCOPE",
    "K_LITERAL","K_NOTSERIALIZED","K_VALUE","K_NOT_IN_GC_HEAP",
    "K_INTERFACE","K_SEALED","K_ABSTRACT","K_AUTO","K_SEQUENTIAL",
    "K_ANSI","K_UNICODE","K_AUTOCHAR","K_BESTFIT","K_IMPORT",
    "K_SERIALIZABLE","K_NESTED","K_LATEINIT","K_EXTENDS","K_IMPLEMENTS",
    "K_FINAL","K_VIRTUAL","K_HIDEBYSIG","K_NEWSLOT","K_UNMANAGEDEXP",
    "K_PINVOKEIMPL","K_NOMANGLE","K_OLE","K_LASTERR","K_WINAPI",
    "K_NATIVE","K_IL","K_CIL","K_OPTIL","K_MANAGED","K_FORWARDREF",
    "K_RUNTIME","K_INTERNALCALL","K_SYNCHRONIZED","K_NOINLINING",
    "K_CUSTOM","K_FIXED","K_SYSSTRING","K_ARRAY","K_VARIANT","K_CURRENCY",
    "K_SYSCHAR","K_VOID","K_BOOL","K_INT8","K_INT16","K_INT32","K_INT64",
    "K_FLOAT32","K_FLOAT64","K_ERROR","K_UNSIGNED","K_UINT","K_UINT8",
    "K_UINT16","K_UINT32","K_UINT64","K_DECIMAL","K_DATE","K_BSTR",
    "K_LPSTR","K_LPWSTR","K_LPTSTR","K_OBJECTREF","K_IUNKNOWN",
    "K_IDISPATCH","K_STRUCT","K_SAFEARRAY","K_INT","K_BYVALSTR","K_TBSTR",
    "K_LPVOID","K_ANY","K_FLOAT","K_LPSTRUCT","K_NULL","K_PTR","K_VECTOR",
    "K_HRESULT","K_CARRAY","K_USERDEFINED","K_RECORD","K_FILETIME",
    "K_BLOB","K_STREAM","K_STORAGE","K_STREAMED_OBJECT","K_STORED_OBJECT",
    "K_BLOB_OBJECT","K_CF","K_CLSID","K_METHOD","K_CLASS","K_PINNED",
    "K_MODREQ","K_MODOPT","K_TYPEDREF","K_TYPE","K_WCHAR","K_CHAR",
    "K_FROMUNMANAGED","K_CALLMOSTDERIVED","K_BYTEARRAY","K_WITH","K_INIT",
    "K_TO","K_CATCH","K_FILTER","K_FINALLY","K_FAULT","K_HANDLER","K_TLS",
    "K_FIELD","K_PROPERTY","K_REQUEST","K_DEMAND","K_ASSERT","K_DENY",
    "K_PERMITONLY","K_LINKCHECK","K_INHERITCHECK","K_REQMIN","K_REQOPT",
    "K_REQREFUSE","K_PREJITGRANT","K_PREJITDENY","K_NONCASDEMAND",
    "K_NONCASLINKDEMAND","K_NONCASINHERITANCE","K_READONLY",
    "K_NOMETADATA","K_ALGORITHM","K_FULLORIGIN","K_ENABLEJITTRACKING",
    "K_DISABLEJITOPTIMIZER","K_RETARGETABLE","K_PRESERVESIG",
    "K_BEFOREFIELDINIT","K_ALIGNMENT","K_NULLREF","K_VALUETYPE",
    "K_COMPILERCONTROLLED","K_REQSECOBJ","K_ENUM","K_OBJECT","K_STRING",
    "K_TRUE","K_FALSE","K_IS","K_ON","K_OFF","K_FORWARDER",
    "K_CHARMAPERROR","K_LEGACY","K_LIBRARY",
  };

  /** index-checked interface to yyNames[].
      @param token single character or %token value.
      @return token name or [illegal] or [unknown].
    */
  public static string yyname (int token) {
    if ((token < 0) || (token > yyNames.Length)) return "[illegal]";
    string name;
    if ((name = yyNames[token]) != null) return name;
    return "[unknown]";
  }

#pragma warning disable 414
  int yyExpectingState;
#pragma warning restore 414
  /** computes list of expected tokens on error by tracing the tables.
      @param state for which to compute the list.
      @return list of token names.
    */
  protected int [] yyExpectingTokens (int state){
    int token, n, len = 0;
    bool[] ok = new bool[yyNames.Length];
    if ((n = yySindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    if ((n = yyRindex[state]) != 0)
      for (token = n < 0 ? -n : 0;
           (token < yyNames.Length) && (n+token < yyTable.Length); ++ token)
        if (yyCheck[n+token] == token && !ok[token] && yyNames[token] != null) {
          ++ len;
          ok[token] = true;
        }
    int [] result = new int [len];
    for (n = token = 0; n < len;  ++ token)
      if (ok[token]) result[n++] = token;
    return result;
  }
  protected string[] yyExpecting (int state) {
    int [] tokens = yyExpectingTokens (state);
    string [] result = new string[tokens.Length];
    for (int n = 0; n < tokens.Length;  n++)
      result[n++] = yyNames[tokens [n]];
    return result;
  }

  /** the generated parser, with debugging messages.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @param yydebug debug message writer implementing yyDebug, or null.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex, Object yyd)
				 {
    this.debug = (yydebug.yyDebug)yyd;
    return yyparse(yyLex);
  }

  /** initial size and increment of the state/value stack [default 256].
      This is not final so that it can be overwritten outside of invocations
      of yyparse().
    */
  protected int yyMax;

  /** executed at the beginning of a reduce action.
      Used as $$ = yyDefault($1), prior to the user-specified action, if any.
      Can be overwritten to provide deep copy, etc.
      @param first value for $1, or null.
      @return first.
    */
  protected Object yyDefault (Object first) {
    return first;
  }

	static int[] global_yyStates;
	static object[] global_yyVals;
#pragma warning disable 649
	protected bool use_global_stacks;
#pragma warning restore 649
	object[] yyVals;					// value stack
	object yyVal;						// value stack ptr
	int yyToken;						// current input
	int yyTop;

  /** the generated parser.
      Maintains a state and a value stack, currently with fixed maximum size.
      @param yyLex scanner.
      @return result of the last reduction, if any.
      @throws yyException on irrecoverable parse error.
    */
  internal Object yyparse (yyParser.yyInput yyLex)
  {
    if (yyMax <= 0) yyMax = 256;		// initial size
    int yyState = 0;                   // state stack ptr
    int [] yyStates;               	// state stack 
    yyVal = null;
    yyToken = -1;
    int yyErrorFlag = 0;				// #tks to shift
	if (use_global_stacks && global_yyStates != null) {
		yyVals = global_yyVals;
		yyStates = global_yyStates;
   } else {
		yyVals = new object [yyMax];
		yyStates = new int [yyMax];
		if (use_global_stacks) {
			global_yyVals = yyVals;
			global_yyStates = yyStates;
		}
	}

    /*yyLoop:*/ for (yyTop = 0;; ++ yyTop) {
      if (yyTop >= yyStates.Length) {			// dynamically increase
        global::System.Array.Resize (ref yyStates, yyStates.Length+yyMax);
        global::System.Array.Resize (ref yyVals, yyVals.Length+yyMax);
      }
      yyStates[yyTop] = yyState;
      yyVals[yyTop] = yyVal;
      if (debug != null) debug.push(yyState, yyVal);

      /*yyDiscarded:*/ while (true) {	// discarding a token does not change stack
        int yyN;
        if ((yyN = yyDefRed[yyState]) == 0) {	// else [default] reduce (yyN)
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
              debug.lex(yyState, yyToken, yyname(yyToken), yyLex.value());
          }
          if ((yyN = yySindex[yyState]) != 0 && ((yyN += yyToken) >= 0)
              && (yyN < yyTable.Length) && (yyCheck[yyN] == yyToken)) {
            if (debug != null)
              debug.shift(yyState, yyTable[yyN], yyErrorFlag-1);
            yyState = yyTable[yyN];		// shift to yyN
            yyVal = yyLex.value();
            yyToken = -1;
            if (yyErrorFlag > 0) -- yyErrorFlag;
            goto continue_yyLoop;
          }
          if ((yyN = yyRindex[yyState]) != 0 && (yyN += yyToken) >= 0
              && yyN < yyTable.Length && yyCheck[yyN] == yyToken)
            yyN = yyTable[yyN];			// reduce (yyN)
          else
            switch (yyErrorFlag) {
  
            case 0:
              yyExpectingState = yyState;
              // yyerror(String.Format ("syntax error, got token `{0}'", yyname (yyToken)), yyExpecting(yyState));
              if (debug != null) debug.error("syntax error");
              if (yyToken == 0 /*eof*/ || yyToken == eof_token) throw new yyParser.yyUnexpectedEof ();
              goto case 1;
            case 1: case 2:
              yyErrorFlag = 3;
              do {
                if ((yyN = yySindex[yyStates[yyTop]]) != 0
                    && (yyN += Token.yyErrorCode) >= 0 && yyN < yyTable.Length
                    && yyCheck[yyN] == Token.yyErrorCode) {
                  if (debug != null)
                    debug.shift(yyStates[yyTop], yyTable[yyN], 3);
                  yyState = yyTable[yyN];
                  yyVal = yyLex.value();
                  goto continue_yyLoop;
                }
                if (debug != null) debug.pop(yyStates[yyTop]);
              } while (-- yyTop >= 0);
              if (debug != null) debug.reject();
              throw new yyParser.yyException("irrecoverable syntax error");
  
            case 3:
              if (yyToken == 0) {
                if (debug != null) debug.reject();
                throw new yyParser.yyException("irrecoverable syntax error at end-of-file");
              }
              if (debug != null)
                debug.discard(yyState, yyToken, yyname(yyToken),
  							yyLex.value());
              yyToken = -1;
              goto continue_yyDiscarded;		// leave stack alone
            }
        }
        int yyV = yyTop + 1-yyLen[yyN];
        if (debug != null)
          debug.reduce(yyState, yyStates[yyV-1], yyN, YYRules.getRule (yyN), yyLen[yyN]);
        yyVal = yyV > yyTop ? null : yyVals[yyV]; // yyVal = yyDefault(yyV > yyTop ? null : yyVals[yyV]);
        switch (yyN) {
case 17:
  case_17();
  break;
case 18:
#line 525 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetSubSystem ((int) yyVals[0+yyTop]);
                          }
  break;
case 19:
#line 529 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetCorFlags ((int) yyVals[0+yyTop]);
                          }
  break;
case 21:
#line 534 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetImageBase ((long) yyVals[0+yyTop]);
                          }
  break;
case 22:
#line 538 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.SetStackReserve ((long)	yyVals[0+yyTop]);
                          }
  break;
case 38:
#line 569 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentNameSpace = null;
                          }
  break;
case 39:
#line 575 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentNameSpace = (string) yyVals[0+yyTop];
                          }
  break;
case 40:
#line 581 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.EndTypeDef ();
                          }
  break;
case 41:
  case_41();
  break;
case 42:
  case_42();
  break;
case 43:
#line 604 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Public; }
  break;
case 44:
#line 605 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Private; }
  break;
case 45:
#line 606 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedPrivate; }
  break;
case 46:
#line 607 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedPublic; }
  break;
case 47:
#line 608 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamily; }
  break;
case 48:
#line 609 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedAssembly;}
  break;
case 49:
#line 610 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamAndAssem; }
  break;
case 50:
#line 611 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamOrAssem; }
  break;
case 51:
#line 612 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { is_value_class = true; }
  break;
case 52:
#line 613 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { is_enum_class = true; }
  break;
case 53:
#line 614 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Interface; }
  break;
case 54:
#line 615 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Sealed; }
  break;
case 55:
#line 616 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Abstract; }
  break;
case 56:
#line 617 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {  }
  break;
case 57:
#line 618 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.SequentialLayout; }
  break;
case 58:
#line 619 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.ExplicitLayout; }
  break;
case 59:
#line 620 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {  }
  break;
case 60:
#line 621 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.UnicodeClass; }
  break;
case 61:
#line 622 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.AutoClass; }
  break;
case 62:
#line 623 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Import; }
  break;
case 63:
#line 624 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Serializable; }
  break;
case 64:
#line 625 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.BeforeFieldInit; }
  break;
case 65:
#line 626 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.SpecialName; }
  break;
case 66:
#line 627 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.RTSpecialName; }
  break;
case 68:
#line 634 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 71:
  case_71();
  break;
case 72:
  case_72();
  break;
case 74:
#line 660 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 76:
#line 667 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 77:
  case_77();
  break;
case 78:
  case_78();
  break;
case 80:
#line 687 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 81:
  case_81();
  break;
case 82:
  case_82();
  break;
case 83:
#line 708 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
			  }
  break;
case 84:
#line 712 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Object, "System.Object");
                          }
  break;
case 85:
  case_85();
  break;
case 86:
  case_86();
  break;
case 87:
  case_87();
  break;
case 88:
  case_88();
  break;
case 89:
  case_89();
  break;
case 90:
  case_90();
  break;
case 91:
  case_91();
  break;
case 92:
#line 769 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PEAPI.GenericParamAttributes ();
                          }
  break;
case 93:
#line 773 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.Covariant; 
                          }
  break;
case 94:
#line 777 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.Contravariant; 
                          }
  break;
case 95:
#line 781 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.DefaultConstructorConstrait; 
                          }
  break;
case 96:
#line 785 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.NotNullableValueTypeConstraint; 
                          }
  break;
case 97:
#line 789 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                               yyVal = (PEAPI.GenericParamAttributes) yyVals[-1+yyTop] | PEAPI.GenericParamAttributes.ReferenceTypeConstraint; 
                          }
  break;
case 98:
#line 795 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 99:
  case_99();
  break;
case 100:
  case_100();
  break;
case 101:
  case_101();
  break;
case 102:
  case_102();
  break;
case 104:
#line 836 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = String.Format ("{0}/{1}", yyVals[-2+yyTop], yyVals[0+yyTop]);
                          }
  break;
case 105:
  case_105();
  break;
case 106:
  case_106();
  break;
case 107:
  case_107();
  break;
case 116:
#line 880 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				AddSecDecl (yyVals[0+yyTop], false);
			  }
  break;
case 118:
  case_118();
  break;
case 120:
#line 891 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.SetSize ((int) yyVals[0+yyTop]);
                          }
  break;
case 121:
#line 895 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.SetPack ((int) yyVals[0+yyTop]);
                          }
  break;
case 122:
  case_122();
  break;
case 125:
#line 928 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = GetTypeRef ((BaseTypeRef) yyVals[0+yyTop]);
                          }
  break;
case 126:
  case_126();
  break;
case 127:
  case_127();
  break;
case 128:
  case_128();
  break;
case 129:
  case_129();
  break;
case 130:
  case_130();
  break;
case 131:
  case_131();
  break;
case 132:
  case_132();
  break;
case 133:
  case_133();
  break;
case 134:
  case_134();
  break;
case 135:
  case_135();
  break;
case 136:
#line 1002 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new MethodPointerTypeRef ((CallConv) yyVals[-5+yyTop], (BaseTypeRef) yyVals[-4+yyTop], (ArrayList) yyVals[-1+yyTop]);
                          }
  break;
case 138:
#line 1009 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Int8, "System.SByte");
                          }
  break;
case 139:
#line 1013 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Int16, "System.Int16");
                          }
  break;
case 140:
#line 1017 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Int32, "System.Int32");
                          }
  break;
case 141:
#line 1021 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Int64, "System.Int64");
                          }
  break;
case 142:
#line 1025 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Float32, "System.Single");
                          }
  break;
case 143:
#line 1029 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Float64, "System.Double");
                          }
  break;
case 144:
#line 1033 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt8, "System.Byte");
                          }
  break;
case 145:
#line 1037 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt8, "System.Byte");
                          }
  break;
case 146:
#line 1041 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt16, "System.UInt16");     
                          }
  break;
case 147:
#line 1045 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt16, "System.UInt16");     
                          }
  break;
case 148:
#line 1049 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt32, "System.UInt32");
                          }
  break;
case 149:
#line 1053 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt32, "System.UInt32");
                          }
  break;
case 150:
#line 1057 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt64, "System.UInt64");
                          }
  break;
case 151:
#line 1061 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.UInt64, "System.UInt64");
                          }
  break;
case 152:
  case_152();
  break;
case 153:
#line 1070 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.NativeUInt, "System.UIntPtr");
                          }
  break;
case 154:
#line 1074 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.NativeUInt, "System.UIntPtr");
                          }
  break;
case 155:
  case_155();
  break;
case 156:
#line 1083 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Char, "System.Char");
                          }
  break;
case 157:
#line 1087 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new PrimitiveTypeRef (PrimitiveType.Char, "System.Char");
			  }
  break;
case 158:
#line 1091 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Void, "System.Void");
                          }
  break;
case 159:
#line 1095 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.Boolean, "System.Boolean");
                          }
  break;
case 160:
#line 1099 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new PrimitiveTypeRef (PrimitiveType.String, "System.String");
                          }
  break;
case 161:
  case_161();
  break;
case 162:
  case_162();
  break;
case 163:
  case_163();
  break;
case 164:
  case_164();
  break;
case 165:
  case_165();
  break;
case 166:
  case_166();
  break;
case 167:
  case_167();
  break;
case 168:
#line 1156 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (CallConv) yyVals[0+yyTop] | CallConv.Instance;
                          }
  break;
case 169:
#line 1160 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (CallConv) yyVals[0+yyTop] | CallConv.InstanceExplicit;
                          }
  break;
case 171:
#line 1167 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new CallConv ();
                          }
  break;
case 172:
#line 1171 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Default;
                          }
  break;
case 173:
#line 1175 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Vararg;
                          }
  break;
case 174:
#line 1179 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Cdecl;
                          }
  break;
case 175:
#line 1183 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Stdcall;
                          }
  break;
case 176:
#line 1187 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Thiscall;
                          }
  break;
case 177:
#line 1191 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = CallConv.Fastcall;
                          }
  break;
case 179:
#line 1198 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new CustomMarshaller ((string) yyVals[-3+yyTop], (string) yyVals[-1+yyTop]);
			  }
  break;
case 180:
#line 1202 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FixedSysString ((uint) (int)yyVals[-1+yyTop]);
                          }
  break;
case 181:
#line 1206 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FixedArray ((int) yyVals[-1+yyTop]);        
                          }
  break;
case 183:
#line 1211 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Currency;
                          }
  break;
case 185:
#line 1216 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Void;
                          }
  break;
case 186:
#line 1220 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Boolean;
                          }
  break;
case 187:
#line 1224 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int8;
                          }
  break;
case 188:
#line 1228 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int16;
                          }
  break;
case 189:
#line 1232 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int32;
                          }
  break;
case 190:
#line 1236 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int64;
                          }
  break;
case 191:
#line 1240 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Float32;
                          }
  break;
case 192:
#line 1244 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Float64;
                          }
  break;
case 193:
#line 1248 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Error;
                          }
  break;
case 194:
#line 1252 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt8;
                          }
  break;
case 195:
#line 1256 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt8;
                          }
  break;
case 196:
#line 1260 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt16;
                          }
  break;
case 197:
#line 1264 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt16;
                          }
  break;
case 198:
#line 1268 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt32;
                          }
  break;
case 199:
#line 1272 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt32;
                          }
  break;
case 200:
#line 1276 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt64;
                          }
  break;
case 201:
#line 1280 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt64;
                          }
  break;
case 203:
#line 1285 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new NativeArray ((NativeType) yyVals[-2+yyTop]);
			  }
  break;
case 204:
#line 1289 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                		yyVal = new NativeArray ((NativeType) yyVals[-3+yyTop], (int) yyVals[-1+yyTop], 0, 0);
			  }
  break;
case 205:
  case_205();
  break;
case 206:
  case_206();
  break;
case 209:
#line 1305 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.BStr;
                          }
  break;
case 210:
#line 1309 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.LPStr;
                          }
  break;
case 211:
#line 1313 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.LPWStr;
                          }
  break;
case 212:
#line 1317 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.LPTStr;
                          }
  break;
case 214:
#line 1322 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.IUnknown;
                          }
  break;
case 215:
#line 1326 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.IDispatch;
                          }
  break;
case 216:
#line 1330 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Struct;
                          }
  break;
case 217:
#line 1334 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Interface;
                          }
  break;
case 218:
  case_218();
  break;
case 220:
#line 1346 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.Int;
                          }
  break;
case 221:
#line 1350 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.UInt;
                          }
  break;
case 223:
#line 1355 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.ByValStr;
                          }
  break;
case 224:
#line 1359 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.AnsiBStr;
                          }
  break;
case 225:
#line 1363 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.TBstr;
                          }
  break;
case 226:
#line 1367 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.VariantBool;
                          }
  break;
case 227:
#line 1371 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.FuncPtr;
                          }
  break;
case 228:
#line 1375 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.AsAny;
                          }
  break;
case 229:
#line 1379 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = NativeType.LPStruct;
                          }
  break;
case 232:
#line 1387 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.variant;
			  }
  break;
case 233:
#line 1391 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.currency;
			  }
  break;
case 235:
#line 1396 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.boolean;
			  }
  break;
case 236:
#line 1400 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.int8;
			  }
  break;
case 237:
#line 1404 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.int16;
			  }
  break;
case 238:
#line 1408 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.int32;
			  }
  break;
case 240:
#line 1413 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.float32;
			  }
  break;
case 241:
#line 1417 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.float64;
			  }
  break;
case 242:
#line 1421 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.uint8;
			  }
  break;
case 243:
#line 1425 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.uint16;
			  }
  break;
case 244:
#line 1429 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.uint32;
			  }
  break;
case 250:
#line 1438 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.Decimal;
			  }
  break;
case 251:
#line 1442 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.date;
			  }
  break;
case 252:
#line 1446 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.bstr;
			  }
  break;
case 255:
#line 1452 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.unknown;
			  }
  break;
case 256:
#line 1456 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.unknown;
			  }
  break;
case 258:
#line 1461 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.Int;
			  }
  break;
case 259:
#line 1465 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.UInt;
			  }
  break;
case 260:
#line 1469 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = SafeArrayType.error;
			  }
  break;
case 276:
  case_276();
  break;
case 278:
#line 1515 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 279:
#line 1521 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FieldAttr ();
                          }
  break;
case 280:
#line 1525 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Public;
                          }
  break;
case 281:
#line 1529 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Private;
                          }
  break;
case 282:
#line 1533 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Family;
                          }
  break;
case 283:
#line 1537 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Assembly;
                          }
  break;
case 284:
#line 1541 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.FamAndAssem;
                          }
  break;
case 285:
#line 1545 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.FamOrAssem;
                          }
  break;
case 286:
#line 1549 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                /* This is just 0x0000*/
                          }
  break;
case 287:
#line 1553 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Static;
                          }
  break;
case 288:
#line 1557 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Initonly;
                          }
  break;
case 289:
#line 1561 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.RTSpecialName;
                          }
  break;
case 290:
#line 1565 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.SpecialName;
                          }
  break;
case 291:
  case_291();
  break;
case 292:
#line 1574 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Literal;
                          }
  break;
case 293:
#line 1578 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FieldAttr) yyVals[-1+yyTop] | FieldAttr.Notserialized;
                          }
  break;
case 295:
#line 1585 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 297:
#line 1592 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 298:
#line 1598 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FloatConst (Convert.ToSingle (yyVals[-1+yyTop]));
                          }
  break;
case 299:
#line 1602 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new DoubleConst (Convert.ToDouble (yyVals[-1+yyTop]));
                          }
  break;
case 300:
#line 1606 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FloatConst (BitConverter.ToSingle (BitConverter.GetBytes ((long)yyVals[-1+yyTop]), BitConverter.IsLittleEndian ? 0 : 4));
                          }
  break;
case 301:
#line 1610 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new DoubleConst (BitConverter.Int64BitsToDouble ((long)yyVals[-1+yyTop]));
                          }
  break;
case 302:
#line 1614 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new IntConst (Convert.ToInt64 (yyVals[-1+yyTop]));
                          }
  break;
case 303:
#line 1618 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new UIntConst (Convert.ToUInt64 ((ulong)(long) yyVals[-1+yyTop]));
                          }
  break;
case 304:
#line 1622 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new IntConst ((int)((long)yyVals[-1+yyTop]));
                          }
  break;
case 305:
#line 1626 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new UIntConst ((uint)((long)yyVals[-1+yyTop]));
                          }
  break;
case 306:
#line 1630 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new IntConst ((short)((long) yyVals[-1+yyTop]));
                          }
  break;
case 307:
#line 1634 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new UIntConst ((ushort)((long) yyVals[-1+yyTop]));
                          }
  break;
case 308:
#line 1638 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new CharConst (Convert.ToChar (yyVals[-1+yyTop]));
                          }
  break;
case 309:
#line 1642 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new CharConst (Convert.ToChar (yyVals[-1+yyTop]));
			  }
  break;
case 310:
#line 1646 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new IntConst ((sbyte)((long) (yyVals[-1+yyTop])));
                          }
  break;
case 311:
#line 1650 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new UIntConst ((byte)((long) (yyVals[-1+yyTop])));
                          }
  break;
case 312:
#line 1654 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new BoolConst ((bool) yyVals[-1+yyTop]);
                          }
  break;
case 314:
#line 1661 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ByteArrConst ((byte[]) yyVals[0+yyTop]);
                          }
  break;
case 316:
  case_316();
  break;
case 317:
#line 1673 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new NullConst ();
                          }
  break;
case 319:
#line 1680 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new StringConst ((string) yyVals[-1+yyTop]);
			  }
  break;
case 321:
#line 1691 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				  	yyVal = new List<BoolConst> () { new BoolConst ((bool) yyVals[0+yyTop]) };
				  }
  break;
case 322:
  case_322();
  break;
case 323:
  case_323();
  break;
case 324:
#line 1722 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new DataDef ((string) yyVals[-1+yyTop], (bool) yyVals[-2+yyTop]);    
                          }
  break;
case 325:
#line 1726 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new DataDef (String.Empty, (bool) yyVals[0+yyTop]);
                          }
  break;
case 326:
#line 1729 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = false; }
  break;
case 327:
#line 1730 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = true; }
  break;
case 328:
#line 1736 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 330:
  case_330();
  break;
case 331:
  case_331();
  break;
case 332:
#line 1756 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new StringConst ((string) yyVals[-1+yyTop]);
                          }
  break;
case 333:
#line 1760 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new StringConst ((string) yyVals[-1+yyTop]);
			  }
  break;
case 334:
  case_334();
  break;
case 335:
#line 1769 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ByteArrConst ((byte[]) yyVals[0+yyTop]);
                          }
  break;
case 336:
  case_336();
  break;
case 337:
  case_337();
  break;
case 338:
  case_338();
  break;
case 339:
  case_339();
  break;
case 340:
  case_340();
  break;
case 341:
  case_341();
  break;
case 342:
  case_342();
  break;
case 343:
  case_343();
  break;
case 344:
  case_344();
  break;
case 345:
  case_345();
  break;
case 346:
  case_346();
  break;
case 347:
  case_347();
  break;
case 348:
  case_348();
  break;
case 349:
#line 1891 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.EndMethodDef (tokenizer.Location);
                          }
  break;
case 350:
  case_350();
  break;
case 351:
  case_351();
  break;
case 352:
#line 1930 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new MethAttr (); }
  break;
case 353:
#line 1931 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Static; }
  break;
case 354:
#line 1932 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Public; }
  break;
case 355:
#line 1933 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Private; }
  break;
case 356:
#line 1934 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Family; }
  break;
case 357:
#line 1935 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Assembly; }
  break;
case 358:
#line 1936 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.FamAndAssem; }
  break;
case 359:
#line 1937 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.FamOrAssem; }
  break;
case 360:
#line 1938 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { /* CHECK HEADERS */ }
  break;
case 361:
#line 1939 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Final; }
  break;
case 362:
#line 1940 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Virtual; }
  break;
case 363:
#line 1941 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Abstract; }
  break;
case 364:
#line 1942 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.HideBySig; }
  break;
case 365:
#line 1943 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.NewSlot; }
  break;
case 366:
#line 1944 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.RequireSecObject; }
  break;
case 367:
#line 1945 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.SpecialName; }
  break;
case 368:
#line 1946 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.RTSpecialName; }
  break;
case 369:
#line 1947 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (MethAttr) yyVals[-1+yyTop] | MethAttr.Strict; }
  break;
case 370:
#line 1948 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { /* Do nothing */ }
  break;
case 372:
  case_372();
  break;
case 373:
  case_373();
  break;
case 374:
  case_374();
  break;
case 375:
#line 1974 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new PInvokeAttr (); }
  break;
case 376:
#line 1975 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.nomangle; }
  break;
case 377:
#line 1976 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.ansi; }
  break;
case 378:
#line 1977 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.unicode; }
  break;
case 379:
#line 1978 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.autochar; }
  break;
case 380:
#line 1979 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.lasterr; }
  break;
case 381:
#line 1980 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.winapi; }
  break;
case 382:
#line 1981 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.cdecl; }
  break;
case 383:
#line 1982 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.stdcall; }
  break;
case 384:
#line 1983 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.thiscall; }
  break;
case 385:
#line 1984 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-1+yyTop] | PInvokeAttr.fastcall; }
  break;
case 386:
#line 1985 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-3+yyTop] | PInvokeAttr.bestfit_on; }
  break;
case 387:
#line 1986 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-3+yyTop] | PInvokeAttr.bestfit_off; }
  break;
case 388:
#line 1987 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-3+yyTop] | PInvokeAttr.charmaperror_on; }
  break;
case 389:
#line 1988 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (PInvokeAttr) yyVals[-3+yyTop] | PInvokeAttr.charmaperror_off; }
  break;
case 393:
#line 1996 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new ParamAttr (); }
  break;
case 394:
#line 1997 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ParamAttr) yyVals[-3+yyTop] | ParamAttr.In; }
  break;
case 395:
#line 1998 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ParamAttr) yyVals[-3+yyTop] | ParamAttr.Out; }
  break;
case 396:
#line 1999 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ParamAttr) yyVals[-3+yyTop] | ParamAttr.Opt; }
  break;
case 397:
#line 2002 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new ImplAttr (); }
  break;
case 398:
#line 2003 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Native; }
  break;
case 399:
#line 2004 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.IL; }
  break;
case 400:
#line 2005 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.IL; }
  break;
case 401:
#line 2006 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Optil; }
  break;
case 402:
#line 2007 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { /* should this reset? */ }
  break;
case 403:
#line 2008 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Unmanaged; }
  break;
case 404:
#line 2009 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.ForwardRef; }
  break;
case 405:
#line 2010 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.PreserveSig; }
  break;
case 406:
#line 2011 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Runtime; }
  break;
case 407:
#line 2012 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.InternalCall; }
  break;
case 408:
#line 2013 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.Synchronised; }
  break;
case 409:
#line 2014 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (ImplAttr) yyVals[-1+yyTop] | ImplAttr.NoInLining; }
  break;
case 412:
  case_412();
  break;
case 413:
  case_413();
  break;
case 414:
#line 2038 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ParamDef ((ParamAttr) yyVals[-1+yyTop], null, (BaseTypeRef) yyVals[0+yyTop]);
                          }
  break;
case 415:
#line 2042 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ParamDef ((ParamAttr) yyVals[-2+yyTop], (string) yyVals[0+yyTop], (BaseTypeRef) yyVals[-1+yyTop]);
                          }
  break;
case 416:
  case_416();
  break;
case 417:
  case_417();
  break;
case 418:
  case_418();
  break;
case 419:
#line 2067 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new ArrayList (0);
                          }
  break;
case 420:
  case_420();
  break;
case 421:
  case_421();
  break;
case 422:
  case_422();
  break;
case 423:
  case_423();
  break;
case 428:
  case_428();
  break;
case 429:
#line 2112 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentMethodDef.SetMaxStack ((int) yyVals[0+yyTop]);
                          }
  break;
case 430:
  case_430();
  break;
case 431:
  case_431();
  break;
case 432:
  case_432();
  break;
case 433:
#line 2136 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentMethodDef.ZeroInit ();
                          }
  break;
case 437:
  case_437();
  break;
case 438:
  case_438();
  break;
case 439:
  case_439();
  break;
case 441:
  case_441();
  break;
case 443:
#line 2192 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentMethodDef.AddLabel ((string) yyVals[-1+yyTop]);
                          }
  break;
case 446:
#line 2198 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				AddSecDecl (yyVals[0+yyTop], false);
			  }
  break;
case 449:
  case_449();
  break;
case 452:
  case_452();
  break;
case 453:
  case_453();
  break;
case 454:
#line 2226 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new Local (-1, (BaseTypeRef) yyVals[0+yyTop]);
                          }
  break;
case 455:
#line 2230 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new Local (-1, (string) yyVals[0+yyTop], (BaseTypeRef) yyVals[-1+yyTop]);
                          }
  break;
case 456:
#line 2234 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new Local ((int) yyVals[-1+yyTop], (BaseTypeRef) yyVals[0+yyTop]);
                          }
  break;
case 457:
#line 2238 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new Local ((int) yyVals[-2+yyTop], (string) yyVals[0+yyTop], (BaseTypeRef) yyVals[-1+yyTop]);
                          }
  break;
case 458:
#line 2244 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[-1+yyTop];
                          }
  break;
case 459:
  case_459();
  break;
case 460:
  case_460();
  break;
case 462:
  case_462();
  break;
case 463:
  case_463();
  break;
case 464:
  case_464();
  break;
case 465:
#line 2297 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new TryBlock ((HandlerBlock) yyVals[0+yyTop], tokenizer.Location);
                          }
  break;
case 466:
  case_466();
  break;
case 467:
  case_467();
  break;
case 468:
  case_468();
  break;
case 469:
  case_469();
  break;
case 470:
  case_470();
  break;
case 471:
  case_471();
  break;
case 472:
  case_472();
  break;
case 473:
  case_473();
  break;
case 474:
  case_474();
  break;
case 475:
  case_475();
  break;
case 476:
  case_476();
  break;
case 477:
#line 2379 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                
                          }
  break;
case 478:
  case_478();
  break;
case 479:
  case_479();
  break;
case 480:
  case_480();
  break;
case 481:
  case_481();
  break;
case 482:
  case_482();
  break;
case 483:
  case_483();
  break;
case 484:
  case_484();
  break;
case 485:
  case_485();
  break;
case 486:
  case_486();
  break;
case 487:
  case_487();
  break;
case 488:
  case_488();
  break;
case 489:
  case_489();
  break;
case 490:
  case_490();
  break;
case 491:
  case_491();
  break;
case 492:
  case_492();
  break;
case 493:
  case_493();
  break;
case 494:
  case_494();
  break;
case 495:
  case_495();
  break;
case 496:
  case_496();
  break;
case 497:
  case_497();
  break;
case 498:
  case_498();
  break;
case 499:
  case_499();
  break;
case 500:
  case_500();
  break;
case 501:
  case_501();
  break;
case 502:
#line 2577 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentMethodDef.AddInstr (new SwitchInstr ((ArrayList) yyVals[-1+yyTop], tokenizer.Location));
                          }
  break;
case 503:
  case_503();
  break;
case 504:
  case_504();
  break;
case 506:
  case_506();
  break;
case 507:
  case_507();
  break;
case 508:
  case_508();
  break;
case 509:
  case_509();
  break;
case 512:
#line 2667 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = yyVals[0+yyTop];
                          }
  break;
case 513:
  case_513();
  break;
case 514:
#line 2678 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = codegen.GetGlobalFieldRef ((BaseTypeRef) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);
                          }
  break;
case 515:
#line 2684 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.EndEventDef ();
                          }
  break;
case 516:
  case_516();
  break;
case 518:
#line 2700 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FeatureAttr ();
                          }
  break;
case 519:
#line 2704 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] & FeatureAttr.Rtspecialname;
                          }
  break;
case 520:
#line 2708 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] & FeatureAttr.Specialname;
                          }
  break;
case 523:
  case_523();
  break;
case 524:
  case_524();
  break;
case 525:
  case_525();
  break;
case 526:
  case_526();
  break;
case 527:
  case_527();
  break;
case 530:
#line 2747 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.EndPropertyDef ();
                          }
  break;
case 531:
  case_531();
  break;
case 532:
#line 2766 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new FeatureAttr ();
                          }
  break;
case 533:
#line 2770 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] | FeatureAttr.Rtspecialname;
                          }
  break;
case 534:
#line 2774 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] | FeatureAttr.Specialname;
                          }
  break;
case 535:
#line 2778 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (FeatureAttr) yyVals[-1+yyTop] | FeatureAttr.Instance;
                          }
  break;
case 538:
#line 2788 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.CurrentProperty.AddSet ((MethodRef) yyVals[0+yyTop]);
                          }
  break;
case 539:
#line 2792 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.CurrentProperty.AddGet ((MethodRef) yyVals[0+yyTop]);
                          }
  break;
case 540:
#line 2796 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentTypeDef.CurrentProperty.AddOther ((MethodRef) yyVals[0+yyTop]);
                          }
  break;
case 541:
  case_541();
  break;
case 544:
#line 2810 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new CustomAttr ((BaseMethodRef) yyVals[0+yyTop], null);
                          }
  break;
case 546:
#line 2815 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = new CustomAttr ((BaseMethodRef) yyVals[-2+yyTop], new ByteArrConst ((byte[]) yyVals[0+yyTop]));
                          }
  break;
case 547:
#line 2819 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
            	yyVal = new CustomAttr ((BaseMethodRef) yyVals[-4+yyTop], (PEAPI.Constant) yyVals[-1+yyTop]);
              }
  break;
case 551:
  case_551();
  break;
case 552:
#line 2840 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
    				yyVal = new ByteArrConst ((byte[]) yyVals[0+yyTop]);
                 }
  break;
case 553:
#line 2844 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
					yyVal = new StringConst ((string) yyVals[-1+yyTop]);
				  }
  break;
case 554:
  case_554();
  break;
case 556:
  case_556();
  break;
case 558:
#line 2873 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				  	yyVal = new StringConst (((TypeRef) yyVals[-1+yyTop]).FullName);
				  }
  break;
case 559:
  case_559();
  break;
case 560:
  case_560();
  break;
case 561:
#line 2909 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = TypeSpecToPermPair (yyVals[-4+yyTop], yyVals[-3+yyTop], (ArrayList) yyVals[-1+yyTop]);
			  }
  break;
case 562:
#line 2913 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = TypeSpecToPermPair (yyVals[-1+yyTop], yyVals[0+yyTop], null);
			  }
  break;
case 563:
  case_563();
  break;
case 564:
  case_564();
  break;
case 565:
#line 2930 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new MIPermissionSet ((PEAPI.SecurityAction) yyVals[-4+yyTop], (ArrayList) yyVals[-1+yyTop]);
			  }
  break;
case 566:
  case_566();
  break;
case 567:
  case_567();
  break;
case 568:
#line 2950 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new MIPermission ((BaseTypeRef) yyVals[-4+yyTop], (ArrayList) yyVals[-1+yyTop]);
			  }
  break;
case 569:
  case_569();
  break;
case 570:
  case_570();
  break;
case 571:
  case_571();
  break;
case 572:
  case_572();
  break;
case 573:
#line 2982 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new NameValuePair ((string) yyVals[-2+yyTop], (PEAPI.Constant) yyVals[0+yyTop]);
			  }
  break;
case 574:
#line 2988 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = MemberTypes.Property;
			  }
  break;
case 575:
#line 2992 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = MemberTypes.Field;
			  }
  break;
case 576:
  case_576();
  break;
case 577:
  case_577();
  break;
case 578:
#line 3012 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = new NameValuePair ((string) yyVals[-2+yyTop], yyVals[0+yyTop]);
			  }
  break;
case 581:
#line 3020 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = yyVals[-1+yyTop];
			  }
  break;
case 583:
#line 3025 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = ClassRefToObject (yyVals[-5+yyTop], (byte) (int) yyVals[-1+yyTop]);
			  }
  break;
case 584:
#line 3029 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = ClassRefToObject (yyVals[-5+yyTop], (short) (int) yyVals[-1+yyTop]);
			  }
  break;
case 585:
#line 3033 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = ClassRefToObject (yyVals[-5+yyTop], (int) yyVals[-1+yyTop]);
			  }
  break;
case 586:
#line 3037 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = ClassRefToObject (yyVals[-3+yyTop], (int) yyVals[-1+yyTop]);
			  }
  break;
case 587:
#line 3043 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.Request;
			  }
  break;
case 588:
#line 3047 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.Demand;
			  }
  break;
case 589:
#line 3051 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.Assert;
			  }
  break;
case 590:
#line 3055 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.Deny;
			  }
  break;
case 591:
#line 3059 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.PermitOnly;
			  }
  break;
case 592:
#line 3063 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.LinkDemand;
			  }
  break;
case 593:
#line 3067 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.InheritDemand;
			  }
  break;
case 594:
#line 3071 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.RequestMinimum;
			  }
  break;
case 595:
#line 3075 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.RequestOptional;
			  }
  break;
case 596:
#line 3079 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.RequestRefuse;
			  }
  break;
case 597:
#line 3083 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.PreJitGrant;
			  }
  break;
case 598:
#line 3087 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.PreJitDeny;
			  }
  break;
case 599:
#line 3091 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.NonCasDemand;
			  }
  break;
case 600:
#line 3095 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.NonCasLinkDemand;
			  }
  break;
case 601:
#line 3099 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				yyVal = PEAPI.SecurityAction.NonCasInheritance;
			  }
  break;
case 602:
#line 3105 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                          }
  break;
case 603:
#line 3109 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetModuleName ((string) yyVals[0+yyTop]);
                          }
  break;
case 604:
#line 3113 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.ExternTable.AddModule ((string) yyVals[0+yyTop]);                         
                          }
  break;
case 605:
#line 3120 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.SetFileRef (new FileRef ((string) yyVals[-5+yyTop], (byte []) yyVals[-1+yyTop], (bool) yyVals[-6+yyTop], (bool) yyVals[0+yyTop])); 
                          }
  break;
case 606:
  case_606();
  break;
case 607:
#line 3131 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = true;
                          }
  break;
case 608:
#line 3135 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = false;
                          }
  break;
case 609:
#line 3141 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = false;
                          }
  break;
case 610:
#line 3145 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = true;
                          }
  break;
case 611:
  case_611();
  break;
case 612:
  case_612();
  break;
case 613:
#line 3166 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				  yyVal = new PEAPI.AssemAttr ();
			  }
  break;
case 614:
#line 3173 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				  yyVal = ((PEAPI.AssemAttr) yyVals[-1+yyTop]) | PEAPI.AssemAttr.Retargetable;
			  }
  break;
case 617:
#line 3183 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.SetPublicKey ((byte []) yyVals[0+yyTop]);
			  }
  break;
case 618:
#line 3187 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.SetVersion ((int) yyVals[-6+yyTop], (int) yyVals[-4+yyTop], (int) yyVals[-2+yyTop], (int) yyVals[0+yyTop]);
			  }
  break;
case 619:
#line 3191 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.SetLocale ((string) yyVals[0+yyTop]);
			  }
  break;
case 621:
#line 3196 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.SetHashAlgorithm ((int) yyVals[0+yyTop]);
			  }
  break;
case 622:
#line 3200 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				codegen.ThisAssembly.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
			  }
  break;
case 623:
#line 3204 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
				AddSecDecl (yyVals[0+yyTop], true);
			  }
  break;
case 630:
  case_630();
  break;
case 631:
  case_631();
  break;
case 634:
#line 3240 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetVersion ((int) yyVals[-6+yyTop], (int) yyVals[-4+yyTop], (int) yyVals[-2+yyTop], (int) yyVals[0+yyTop]);
                          }
  break;
case 635:
#line 3244 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetPublicKey ((byte []) yyVals[0+yyTop]);
                          }
  break;
case 636:
#line 3248 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetPublicKeyToken ((byte []) yyVals[0+yyTop]);
                          }
  break;
case 637:
#line 3252 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetLocale ((string) yyVals[0+yyTop]);
                          }
  break;
case 639:
#line 3258 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                codegen.CurrentAssemblyRef.SetHash ((byte []) yyVals[0+yyTop]);
                          }
  break;
case 640:
  case_640();
  break;
case 643:
#line 3273 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
						current_extern = new KeyValuePair<string, TypeAttr> ((string) yyVals[0+yyTop], (TypeAttr) yyVals[-1+yyTop]);
					}
  break;
case 644:
#line 3276 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = 0; }
  break;
case 645:
#line 3277 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Private; }
  break;
case 646:
#line 3278 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Public; }
  break;
case 647:
#line 3279 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-1+yyTop] | TypeAttr.Forwarder; }
  break;
case 648:
#line 3280 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedPublic; }
  break;
case 649:
#line 3281 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedPrivate; }
  break;
case 650:
#line 3282 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamily; }
  break;
case 651:
#line 3283 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedAssembly;}
  break;
case 652:
#line 3284 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamAndAssem; }
  break;
case 653:
#line 3285 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = (TypeAttr)yyVals[-2+yyTop] | TypeAttr.NestedFamOrAssem; }
  break;
case 659:
#line 3298 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
			  	codegen.ExternTable.AddClass (current_extern.Key, current_extern.Value, (string) yyVals[0+yyTop]);
			  }
  break;
case 661:
  case_661();
  break;
case 663:
#line 3316 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = ManifestResource.PublicResource; }
  break;
case 664:
#line 3317 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = ManifestResource.PrivateResource; }
  break;
case 671:
#line 3330 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = String.Format ("{0}{1}", yyVals[-2+yyTop], yyVals[0+yyTop]); }
  break;
case 672:
  case_672();
  break;
case 675:
  case_675();
  break;
case 676:
  case_676();
  break;
case 677:
  case_677();
  break;
case 678:
  case_678();
  break;
case 679:
#line 3369 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { }
  break;
case 680:
#line 3375 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                tokenizer.InByteArray = true;
                          }
  break;
case 681:
  case_681();
  break;
case 682:
#line 3383 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  { yyVal = new byte[0]; }
  break;
case 683:
  case_683();
  break;
case 684:
  case_684();
  break;
case 685:
  case_685();
  break;
case 686:
#line 3407 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = true;
                          }
  break;
case 687:
#line 3411 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = false;
                          }
  break;
case 691:
#line 3422 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
  {
                                yyVal = (string) yyVals[-2+yyTop] + '.' + (string) yyVals[0+yyTop];
                          }
  break;
#line default
        }
        yyTop -= yyLen[yyN];
        yyState = yyStates[yyTop];
        int yyM = yyLhs[yyN];
        if (yyState == 0 && yyM == 0) {
          if (debug != null) debug.shift(0, yyFinal);
          yyState = yyFinal;
          if (yyToken < 0) {
            yyToken = yyLex.advance() ? yyLex.token() : 0;
            if (debug != null)
               debug.lex(yyState, yyToken,yyname(yyToken), yyLex.value());
          }
          if (yyToken == 0) {
            if (debug != null) debug.accept(yyVal);
            return yyVal;
          }
          goto continue_yyLoop;
        }
        if (((yyN = yyGindex[yyM]) != 0) && ((yyN += yyState) >= 0)
            && (yyN < yyTable.Length) && (yyCheck[yyN] == yyState))
          yyState = yyTable[yyN];
        else
          yyState = yyDgoto[yyM];
        if (debug != null) debug.shift(yyStates[yyTop], yyState);
	 goto continue_yyLoop;
      continue_yyDiscarded: ;	// implements the named-loop continue: 'continue yyDiscarded'
      }
    continue_yyLoop: ;		// implements the named-loop continue: 'continue yyLoop'
    }
  }

/*
 All more than 3 lines long rules are wrapped into a method
*/
void case_17()
#line 518 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				if (codegen.CurrentCustomAttrTarget != null)
					codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
			  }

void case_41()
#line 586 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.BeginTypeDef ((TypeAttr) yyVals[-4+yyTop], (string) yyVals[-3+yyTop], 
						yyVals[-1+yyTop] as BaseClassRef, yyVals[0+yyTop] as ArrayList, null, (GenericParameters) yyVals[-2+yyTop]);
				
				if (is_value_class)
					codegen.CurrentTypeDef.MakeValueClass ();
				if (is_enum_class)
					codegen.CurrentTypeDef.MakeEnumClass ();
                          }

void case_42()
#line 598 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{ 
				/* Reset some flags*/
				is_value_class = false;
				is_enum_class = false;
				yyVal = new TypeAttr ();
			  }

void case_71()
#line 642 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList al = new ArrayList ();
                                al.Add (yyVals[0+yyTop]);
                                yyVal = al;
                          }

void case_72()
#line 648 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList al = (ArrayList) yyVals[-2+yyTop];

                                al.Insert (0, yyVals[0+yyTop]);
                                yyVal = al;
                          }

void case_77()
#line 671 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                GenericArguments ga = new GenericArguments ();
                                ga.Add ((BaseTypeRef) yyVals[0+yyTop]);
                                yyVal = ga;
                          }

void case_78()
#line 677 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ((GenericArguments) yyVals[-2+yyTop]).Add ((BaseTypeRef) yyVals[0+yyTop]);
                                yyVal = yyVals[-2+yyTop];
                          }

void case_81()
#line 692 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList al = new ArrayList ();
                                al.Add (yyVals[0+yyTop]);
                                yyVal = al;
                           }

void case_82()
#line 698 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList al = (ArrayList) yyVals[-2+yyTop];
                                al.Add (yyVals[0+yyTop]);
                                yyVal = al;
                          }

void case_85()
#line 714 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[0+yyTop] != null)
                                        yyVal = ((BaseClassRef) yyVals[-1+yyTop]).GetGenericTypeInst ((GenericArguments) yyVals[0+yyTop]);
                                else
                                        yyVal = yyVals[-1+yyTop];
			  }

void case_86()
#line 721 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                GenParam gpar = new GenParam ((int) yyVals[0+yyTop], "", GenParamType.Var);
                                yyVal = new GenericParamRef (gpar, yyVals[0+yyTop].ToString ());
                          }

void case_87()
#line 726 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                GenParam gpar = new GenParam ((int) yyVals[0+yyTop], "", GenParamType.MVar);
                                yyVal = new GenericParamRef (gpar, yyVals[0+yyTop].ToString ());
                          }

void case_88()
#line 731 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				int num = -1;
				string name = (string) yyVals[0+yyTop];
				if (codegen.CurrentTypeDef != null)
					num = codegen.CurrentTypeDef.GetGenericParamNum (name);
				GenParam gpar = new GenParam (num, name, GenParamType.Var);
                                yyVal = new GenericParamRef (gpar, name);
                          }

void case_89()
#line 740 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				int num = -1;
				string name = (string) yyVals[0+yyTop];
				if (codegen.CurrentMethodDef != null)
					num = codegen.CurrentMethodDef.GetGenericParamNum (name);
				GenParam gpar = new GenParam (num, name, GenParamType.MVar);
                                yyVal = new GenericParamRef (gpar, name);
                          }

void case_90()
#line 751 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                GenericParameter gp = new GenericParameter ((string) yyVals[0+yyTop], (PEAPI.GenericParamAttributes) yyVals[-2+yyTop], (ArrayList) yyVals[-1+yyTop]);

                                GenericParameters colln = new GenericParameters ();
                                colln.Add (gp);
                                yyVal = colln;
                          }

void case_91()
#line 759 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                GenericParameters colln = (GenericParameters) yyVals[-4+yyTop];
                                colln.Add (new GenericParameter ((string) yyVals[0+yyTop], (PEAPI.GenericParamAttributes) yyVals[-2+yyTop], (ArrayList) yyVals[-1+yyTop]));
                                yyVal = colln;
                          }

void case_99()
#line 799 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				  if (codegen.CurrentMethodDef != null)
					  codegen.CurrentCustomAttrTarget = codegen.CurrentMethodDef.GetGenericParam ((string) yyVals[0+yyTop]);
				  else
					  codegen.CurrentCustomAttrTarget = codegen.CurrentTypeDef.GetGenericParam ((string) yyVals[0+yyTop]);
				  if (codegen.CurrentCustomAttrTarget == null)
					  Report.Error (String.Format ("Type parameter '{0}' undefined.", (string) yyVals[0+yyTop]));
			  }

void case_100()
#line 808 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				  int index = ((int) yyVals[-1+yyTop]);
				  if (codegen.CurrentMethodDef != null)
					  codegen.CurrentCustomAttrTarget = codegen.CurrentMethodDef.GetGenericParam (index - 1);
				  else
					  codegen.CurrentCustomAttrTarget = codegen.CurrentTypeDef.GetGenericParam (index - 1);
				  if (codegen.CurrentCustomAttrTarget == null)
					  Report.Error (String.Format ("Type parameter '{0}' index out of range.", index));
			  }

void case_101()
#line 820 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList class_list = new ArrayList ();
                                class_list.Add (yyVals[0+yyTop]);
                                yyVal = class_list; 
                          }

void case_102()
#line 826 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList class_list = (ArrayList) yyVals[-2+yyTop];
                                class_list.Add (yyVals[0+yyTop]);
                          }

void case_105()
#line 840 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.IsThisAssembly ((string) yyVals[-2+yyTop])) {
                                        yyVal = codegen.GetTypeRef ((string) yyVals[0+yyTop]);
                                } else {
                                        yyVal = codegen.ExternTable.GetTypeRef ((string) yyVals[-2+yyTop], (string) yyVals[0+yyTop], false);
                                }
                          }

void case_106()
#line 848 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.IsThisModule ((string) yyVals[-2+yyTop])) {
                                        yyVal = codegen.GetTypeRef ((string) yyVals[0+yyTop]);
                                } else {
                                        yyVal = codegen.ExternTable.GetModuleTypeRef ((string) yyVals[-2+yyTop], (string) yyVals[0+yyTop], false);
                                }
                          }

void case_107()
#line 856 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                PrimitiveTypeRef prim = PrimitiveTypeRef.GetPrimitiveType ((string) yyVals[0+yyTop]);

                                if (prim != null && !codegen.IsThisAssembly ("mscorlib"))
                                        yyVal = prim;
                                else
                                        yyVal = codegen.GetTypeRef ((string) yyVals[0+yyTop]);
                          }

void case_118()
#line 883 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                          }

void case_122()
#line 898 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /**/
                                /* My copy of the spec didn't have a type_list but*/
                                /* it seems pretty crucial*/
                                /**/
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-9+yyTop];
                                ArrayList arg_list = (ArrayList) yyVals[0+yyTop];
                                BaseTypeRef[] param_list;
                                BaseMethodRef decl;

                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                decl = owner.GetMethodRef ((BaseTypeRef) yyVals[-4+yyTop],
                                        (CallConv) yyVals[-5+yyTop], (string) yyVals[-7+yyTop], param_list, 0);

				/* NOTICE: `owner' here might be wrong*/
                                string sig = MethodDef.CreateSignature (owner, (CallConv) yyVals[-5+yyTop], (string) yyVals[-1+yyTop],
                                                                        param_list, 0, false);
                                codegen.CurrentTypeDef.AddOverride (sig, decl);                                        
                          }

void case_126()
#line 930 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseClassRef class_ref = (BaseClassRef) yyVals[0+yyTop];
                                class_ref.MakeValueClass ();
                                yyVal = GetTypeRef (class_ref);
                          }

void case_127()
#line 936 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ExternTypeRef ext_ref = codegen.ExternTable.GetTypeRef ((string) yyVals[-3+yyTop], (string) yyVals[-1+yyTop], true);
                                if (yyVals[0+yyTop] != null)
                                        yyVal = ext_ref.GetGenericTypeInst ((GenericArguments) yyVals[0+yyTop]);
                                else
                                        yyVal = GetTypeRef (ext_ref);
                          }

void case_128()
#line 944 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                TypeRef t_ref = codegen.GetTypeRef ((string) yyVals[-1+yyTop]);
                                t_ref.MakeValueClass ();
                                if (yyVals[0+yyTop] != null)
                                        yyVal = t_ref.GetGenericTypeInst ((GenericArguments) yyVals[0+yyTop]);
                                else
                                        yyVal = GetTypeRef (t_ref);
                          }

void case_129()
#line 953 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = (BaseTypeRef) yyVals[-2+yyTop];
                                base_type.MakeArray ();
                                yyVal = base_type;
                          }

void case_130()
#line 959 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = (BaseTypeRef) yyVals[-3+yyTop];
                                ArrayList bound_list = (ArrayList) yyVals[-1+yyTop];
                                base_type.MakeBoundArray (bound_list);
                                yyVal = base_type;
                          }

void case_131()
#line 966 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = (BaseTypeRef) yyVals[-1+yyTop];
                                base_type.MakeManagedPointer ();
                                yyVal = base_type;
                          }

void case_132()
#line 972 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = (BaseTypeRef) yyVals[-1+yyTop];
                                base_type.MakeUnmanagedPointer ();
                                yyVal = base_type;
                          }

void case_133()
#line 978 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = (BaseTypeRef) yyVals[-1+yyTop];
                                base_type.MakePinned ();
                                yyVal = base_type;
                          }

void case_134()
#line 984 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = (BaseTypeRef) yyVals[-4+yyTop];
                                BaseTypeRef class_ref = (BaseTypeRef) yyVals[-1+yyTop];
                                base_type.MakeCustomModified (codegen,
                                        CustomModifier.modreq, class_ref);
                                yyVal = base_type;
                          }

void case_135()
#line 992 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef base_type = (BaseTypeRef) yyVals[-4+yyTop];
                                BaseTypeRef class_ref = (BaseTypeRef) yyVals[-1+yyTop];
                                base_type.MakeCustomModified (codegen,
                                        CustomModifier.modopt, class_ref);
                                yyVal = base_type;
                          }

void case_152()
#line 1063 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* TODO: Is this the proper full name*/
                                yyVal = new PrimitiveTypeRef (PrimitiveType.NativeInt, "System.IntPtr");
                          }

void case_155()
#line 1076 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = new PrimitiveTypeRef (PrimitiveType.TypedRef,
                                        "System.TypedReference");
                          }

void case_161()
#line 1103 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList bound_list = new ArrayList ();
                                bound_list.Add (yyVals[0+yyTop]);
                                yyVal = bound_list;
                          }

void case_162()
#line 1109 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList bound_list = (ArrayList) yyVals[-2+yyTop];
                                bound_list.Add (yyVals[0+yyTop]);
                          }

void case_163()
#line 1116 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* This is shortref for no lowerbound or size*/
                                yyVal = new DictionaryEntry (TypeRef.Ellipsis, TypeRef.Ellipsis);
                          }

void case_164()
#line 1121 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* No lower bound or size*/
                                yyVal = new DictionaryEntry (TypeRef.Ellipsis, TypeRef.Ellipsis);
                          }

void case_165()
#line 1126 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* Only size specified */ 
                                int size = (int) yyVals[0+yyTop];
                                if (size < 0)
                                        /* size cannot be < 0, so emit as (0, ...)
                                           ilasm.net emits it like this */
                                        yyVal = new DictionaryEntry (0, TypeRef.Ellipsis);
                                else
                                        yyVal = new DictionaryEntry (TypeRef.Ellipsis, size);
                          }

void case_166()
#line 1137 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* lower and upper bound*/
                                int lower = (int) yyVals[-2+yyTop];
                                int upper = (int) yyVals[0+yyTop];
                                if (lower > upper) 
                                        Report.Error ("Lower bound " + lower + " must be <= upper bound " + upper);

                                yyVal = new DictionaryEntry (yyVals[-2+yyTop], yyVals[0+yyTop]);
                          }

void case_167()
#line 1147 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* Just lower bound*/
                                yyVal = new DictionaryEntry (yyVals[-1+yyTop], TypeRef.Ellipsis);
                          }

void case_205()
#line 1291 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /*FIXME: Allowed only for methods, !fields*/
                                yyVal = new NativeArray ((NativeType) yyVals[-5+yyTop], (int) yyVals[-3+yyTop], (int) yyVals[-1+yyTop]);
			  }

void case_206()
#line 1296 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /*FIXME: Allowed only for methods, !fields*/
                                yyVal = new NativeArray ((NativeType) yyVals[-4+yyTop], -1, (int) yyVals[-1+yyTop]);
			  }

void case_218()
#line 1336 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[0+yyTop] == null)
                                        yyVal = new SafeArray ();
                                else        
                                        yyVal = new SafeArray ((SafeArrayType) yyVals[0+yyTop]);
                          }

void case_276()
#line 1491 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                FieldDef field_def = new FieldDef((FieldAttr) yyVals[-5+yyTop], 
					(string) yyVals[-3+yyTop], (BaseTypeRef) yyVals[-4+yyTop]);
                                codegen.AddFieldDef (field_def);
                                codegen.CurrentCustomAttrTarget = field_def;
                                
                                if (yyVals[-6+yyTop] != null) {
                                        field_def.SetOffset ((uint) (int)yyVals[-6+yyTop]);
                                }

                                if (yyVals[-2+yyTop] != null) {
                                        field_def.AddDataValue ((string) yyVals[-2+yyTop]);
                                }

                                if (yyVals[-1+yyTop] != null) {
                                        field_def.SetValue ((Constant) yyVals[-1+yyTop]);
                                }
                          }

void case_291()
#line 1567 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                               codegen.AddFieldMarshalInfo ((NativeType) yyVals[-1+yyTop]);
                               yyVal = (FieldAttr) yyVals[-4+yyTop] | FieldAttr.HasFieldMarshal;
                          }

void case_316()
#line 1666 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* ******** THIS IS NOT IN THE DOCUMENTATION ******** //*/
                                yyVal = new StringConst ((string) yyVals[0+yyTop]);
                          }

void case_322()
#line 1693 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				  	var l = (List<BoolConst>) yyVals[-1+yyTop];
				  	l.Add (new BoolConst ((bool) yyVals[0+yyTop]));
				  	yyVal = l;
				  }

void case_323()
#line 1701 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                DataDef datadef = (DataDef) yyVals[-1+yyTop];
                                
                                if (yyVals[0+yyTop] is ArrayList) {
                                        ArrayList const_list = (ArrayList) yyVals[0+yyTop];
                                        DataConstant[] const_arr = new DataConstant[const_list.Count];
                                        
                                        for (int i=0; i<const_arr.Length; i++)
                                                const_arr[i] = (DataConstant) const_list[i];

                                        datadef.PeapiConstant = new ArrayConstant (const_arr);
                                } else {
                                        datadef.PeapiConstant = (PEAPI.Constant) yyVals[0+yyTop];
                                }
                                codegen.AddDataDef (datadef);
                          }

void case_330()
#line 1741 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList dataitem_list = new ArrayList ();
                                dataitem_list.Add (yyVals[0+yyTop]);
                                yyVal = dataitem_list;
                          }

void case_331()
#line 1747 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList list = (ArrayList) yyVals[-2+yyTop];
                                list.Add (yyVals[0+yyTop]);
                          }

void case_334()
#line 1762 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                           /*     DataDef def = codegen.CurrentTypeDef.GetDataDef ((string) $3);*/
                           /*     $$ = new AddressConstant ((DataConstant) def.PeapiConstant);*/
                          }

void case_336()
#line 1771 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* ******** THIS IS NOT IN THE SPECIFICATION ******** //*/
                                yyVal = new ByteArrConst ((byte[]) yyVals[0+yyTop]);
                          }

void case_337()
#line 1776 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                double d = (double) yyVals[-2+yyTop];
                                FloatConst float_const = new FloatConst ((float) d);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (float_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = float_const;
                          }

void case_338()
#line 1786 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                DoubleConst double_const = new DoubleConst ((double) yyVals[-2+yyTop]);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (double_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = double_const;
                          }

void case_339()
#line 1795 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((long) yyVals[-2+yyTop]);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_340()
#line 1804 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((int) yyVals[-2+yyTop]);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_341()
#line 1813 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                int i = (int) yyVals[-2+yyTop];
                                IntConst int_const = new IntConst ((short) i);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_342()
#line 1823 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                int i = (int) yyVals[-2+yyTop];
                                IntConst int_const = new IntConst ((sbyte) i);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_343()
#line 1833 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                FloatConst float_const = new FloatConst (0F);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (float_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = float_const;
                          }

void case_344()
#line 1842 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                DoubleConst double_const = new DoubleConst (0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (double_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = double_const;
                          }

void case_345()
#line 1851 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((long) 0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_346()
#line 1860 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((int) 0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_347()
#line 1869 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((short) 0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_348()
#line 1878 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                IntConst int_const = new IntConst ((sbyte) 0);

                                if (yyVals[0+yyTop] != null)
                                        yyVal = new RepeatedConstant (int_const, (int) yyVals[0+yyTop]);
                                else
                                        yyVal = int_const;
                          }

void case_350()
#line 1896 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                CallConv cc = (CallConv) yyVals[-8+yyTop];
                                if (yyVals[-4+yyTop] != null)
                                        cc |= CallConv.Generic;

                                MethodDef methdef = new MethodDef (
                                        codegen, (MethAttr) yyVals[-9+yyTop], cc,
                                        (ImplAttr) yyVals[0+yyTop], (string) yyVals[-5+yyTop], (BaseTypeRef) yyVals[-6+yyTop],
                                        (ArrayList) yyVals[-2+yyTop], tokenizer.Reader.Location, (GenericParameters) yyVals[-4+yyTop], codegen.CurrentTypeDef);
                                if (pinvoke_info) {
                                        ExternModule mod = codegen.ExternTable.AddModule (pinvoke_mod);
                                        methdef.AddPInvokeInfo (pinvoke_attr, mod, pinvoke_meth);
                                        pinvoke_info = false;
                                }
                          }

void case_351()
#line 1914 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                MethodDef methdef = new MethodDef (
                              		codegen, (MethAttr) yyVals[-12+yyTop], (CallConv) yyVals[-11+yyTop],
                                        (ImplAttr) yyVals[0+yyTop], (string) yyVals[-4+yyTop], (BaseTypeRef) yyVals[-9+yyTop],
                                        (ArrayList) yyVals[-2+yyTop], tokenizer.Reader.Location, null, codegen.CurrentTypeDef);

                                if (pinvoke_info) {
                                        ExternModule mod = codegen.ExternTable.AddModule (pinvoke_mod);
                                        methdef.AddPInvokeInfo (pinvoke_attr, mod, pinvoke_meth);
                                        pinvoke_info = false;
                                }
		                
                                methdef.AddRetTypeMarshalInfo ((NativeType) yyVals[-6+yyTop]);
			  }

void case_372()
#line 1952 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                pinvoke_info = true;
                                pinvoke_mod = (string) yyVals[-4+yyTop];
                                pinvoke_meth = (string) yyVals[-2+yyTop];
                                pinvoke_attr = (PInvokeAttr) yyVals[-1+yyTop];
                          }

void case_373()
#line 1959 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                pinvoke_info = true;
                                pinvoke_mod = (string) yyVals[-2+yyTop];
                                pinvoke_meth = null;
                                pinvoke_attr = (PInvokeAttr) yyVals[-1+yyTop];
                          }

void case_374()
#line 1966 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                pinvoke_info = true;
                                pinvoke_mod = null;
                                pinvoke_meth = null;
                                pinvoke_attr = (PInvokeAttr) yyVals[-1+yyTop];
                          }

void case_412()
#line 2022 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList sig_list = new ArrayList ();
                                sig_list.Add (yyVals[0+yyTop]);
                                yyVal = sig_list;
                          }

void case_413()
#line 2028 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList sig_list = (ArrayList) yyVals[-2+yyTop];
                                sig_list.Add (yyVals[0+yyTop]);
                                yyVal = sig_list;
                          }

void case_416()
#line 2044 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				yyVal = new ParamDef ((ParamAttr) 0, "...", new SentinelTypeRef ());
                                /* $$ = ParamDef.Ellipsis;*/
                          }

void case_417()
#line 2049 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ParamDef param_def = new ParamDef ((ParamAttr) yyVals[-5+yyTop], null, (BaseTypeRef) yyVals[-4+yyTop]);
                                param_def.AddMarshalInfo ((PEAPI.NativeType) yyVals[-1+yyTop]);

                                yyVal = param_def;
			  }

void case_418()
#line 2056 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ParamDef param_def = new ParamDef ((ParamAttr) yyVals[-6+yyTop], (string) yyVals[0+yyTop], (BaseTypeRef) yyVals[-5+yyTop]);
                                param_def.AddMarshalInfo ((PEAPI.NativeType) yyVals[-2+yyTop]);

                                yyVal = param_def;
			  }

void case_420()
#line 2069 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList type_list = new ArrayList ();
                                /* type_list.Add (TypeRef.Ellipsis);*/
				type_list.Add (new SentinelTypeRef ());
                                yyVal = type_list;
                          }

void case_421()
#line 2076 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList type_list = (ArrayList) yyVals[-2+yyTop];
                                /* type_list.Add (TypeRef.Ellipsis);*/
				type_list.Add (new SentinelTypeRef ());
				yyVal = type_list;
                          }

void case_422()
#line 2083 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList type_list = new ArrayList ();
                                type_list.Add (yyVals[-1+yyTop]);
                                yyVal = type_list;
                          }

void case_423()
#line 2089 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList type_list = (ArrayList) yyVals[-4+yyTop];
                                type_list.Add (yyVals[-1+yyTop]);
                          }

void case_428()
#line 2104 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
							codegen.CurrentMethodDef.AddInstr (new
                                        EmitByteInstr ((int) yyVals[0+yyTop], tokenizer.Location));
                          
						}

void case_430()
#line 2114 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[-1+yyTop] != null) {
                                        codegen.CurrentMethodDef.AddLocals (
                                                (ArrayList) yyVals[-1+yyTop]);
                                }
                          }

void case_431()
#line 2121 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[-1+yyTop] != null) {
                                        codegen.CurrentMethodDef.AddLocals (
                                                (ArrayList) yyVals[-1+yyTop]);
                                        codegen.CurrentMethodDef.InitLocals ();
                                }
                          }

void case_432()
#line 2129 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.EntryPoint ();
                                codegen.HasEntryPoint = true;
                          }

void case_437()
#line 2141 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.AddOverride (codegen.CurrentMethodDef,
                                        (BaseTypeRef) yyVals[-2+yyTop], (string) yyVals[0+yyTop]);
                                
                          }

void case_438()
#line 2147 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				codegen.CurrentTypeDef.AddOverride (codegen.CurrentMethodDef.Signature,
					(BaseMethodRef) yyVals[0+yyTop]);
                          }

void case_439()
#line 2154 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-10+yyTop];
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                BaseTypeRef[] param_list;
                                BaseMethodRef methref;

                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                if (owner.UseTypeSpec) {
                                        methref = new TypeSpecMethodRef (owner, (CallConv) yyVals[-12+yyTop], (BaseTypeRef) yyVals[-11+yyTop],
                                                (string) yyVals[-8+yyTop], param_list, (int) yyVals[-5+yyTop]);
                                } else {
                                        methref = owner.GetMethodRef ((BaseTypeRef) yyVals[-11+yyTop],
                                                (CallConv) yyVals[-12+yyTop], (string) yyVals[-8+yyTop], param_list, (int) yyVals[-5+yyTop]);
                                }

				codegen.CurrentTypeDef.AddOverride (codegen.CurrentMethodDef.Signature,
					methref);
			  }

void case_441()
#line 2178 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                int index = (int) yyVals[-2+yyTop];
                                ParamDef param = codegen.CurrentMethodDef.GetParam (index);
                                codegen.CurrentCustomAttrTarget = param;

                                if (param == null) {
                                        Report.Warning (tokenizer.Location, String.Format ("invalid param index ({0}) with .param", index));
                                } else if (yyVals[0+yyTop] != null)
                                        param.AddDefaultValue ((Constant) yyVals[0+yyTop]);
                          }

void case_449()
#line 2202 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                          }

void case_452()
#line 2211 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList local_list = new ArrayList ();
                                local_list.Add (yyVals[0+yyTop]);
                                yyVal = local_list;
                          }

void case_453()
#line 2217 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList local_list = (ArrayList) yyVals[-2+yyTop];
                                local_list.Add (yyVals[0+yyTop]);
                          }

void case_459()
#line 2248 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* This is a reference to a global method in another*/
                                /* assembly. This is not supported in the MS version of ilasm*/
                          }

void case_460()
#line 2253 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                string module = (string) yyVals[-1+yyTop];

                                if (codegen.IsThisModule (module)) {
                                    /* This is not handled yet.*/
                                } else {
                                    yyVal = codegen.ExternTable.GetModuleTypeRef ((string) yyVals[-1+yyTop], "<Module>", false);
                                }

                          }

void case_462()
#line 2267 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = new HandlerBlock ((LabelInfo) yyVals[-2+yyTop],
                                        codegen.CurrentMethodDef.AddLabel ());
                                codegen.CurrentMethodDef.EndLocalsScope ();
                          }

void case_463()
#line 2275 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = codegen.CurrentMethodDef.AddLabel ();
                                codegen.CurrentMethodDef.BeginLocalsScope ();
                          }

void case_464()
#line 2283 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                TryBlock try_block = (TryBlock) yyVals[-1+yyTop];

                                ArrayList clause_list = (ArrayList) yyVals[0+yyTop];
                                foreach (object clause in clause_list)
                                        try_block.AddSehClause ((ISehClause) clause);

                                codegen.CurrentMethodDef.AddInstr (try_block);
                          }

void case_466()
#line 2299 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[-2+yyTop]);
				LabelInfo to = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]);
				
                                yyVal = new TryBlock (new HandlerBlock (from, to), tokenizer.Location);
                          }

void case_467()
#line 2306 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabel ((int) yyVals[-2+yyTop]);
				LabelInfo to = codegen.CurrentMethodDef.AddLabel ((int) yyVals[0+yyTop]);
				
				yyVal = new TryBlock (new HandlerBlock (from, to), tokenizer.Location);
			  }

void case_468()
#line 2315 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList clause_list = new ArrayList ();
                                clause_list.Add (yyVals[0+yyTop]);
                                yyVal = clause_list;
                          }

void case_469()
#line 2321 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList clause_list = (ArrayList) yyVals[-1+yyTop];
                                clause_list.Add (yyVals[0+yyTop]);
                          }

void case_470()
#line 2328 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				if (yyVals[-1+yyTop].GetType () == typeof (PrimitiveTypeRef))
					Report.Error ("Exception not be of a primitive type.");
					
                                BaseTypeRef type = (BaseTypeRef) yyVals[-1+yyTop];
                                CatchBlock cb = new CatchBlock (type);
                                cb.SetHandlerBlock ((HandlerBlock) yyVals[0+yyTop]);
                                yyVal = cb;
                          }

void case_471()
#line 2338 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                FinallyBlock fb = new FinallyBlock ();
                                fb.SetHandlerBlock ((HandlerBlock) yyVals[0+yyTop]);
                                yyVal = fb;
                          }

void case_472()
#line 2344 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                FaultBlock fb = new FaultBlock ();
                                fb.SetHandlerBlock ((HandlerBlock) yyVals[0+yyTop]);
                                yyVal = fb;
                          }

void case_473()
#line 2350 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                FilterBlock fb = (FilterBlock) yyVals[-1+yyTop];
                                fb.SetHandlerBlock ((HandlerBlock) yyVals[0+yyTop]);
                          }

void case_474()
#line 2357 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                HandlerBlock block = (HandlerBlock) yyVals[0+yyTop];
                                FilterBlock fb = new FilterBlock (block);
                                yyVal = fb;
                          }

void case_475()
#line 2363 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]);
                                FilterBlock fb = new FilterBlock (new HandlerBlock (from, null));
                                yyVal = fb;
                          }

void case_476()
#line 2369 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabel ((int) yyVals[0+yyTop]);
				FilterBlock fb = new FilterBlock (new HandlerBlock (from, null));
				yyVal = fb;
			  }

void case_478()
#line 2381 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{	
				LabelInfo from = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[-2+yyTop]);
				LabelInfo to = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]);

                                yyVal = new HandlerBlock (from, to);
                          }

void case_479()
#line 2388 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo from = codegen.CurrentMethodDef.AddLabel ((int) yyVals[-2+yyTop]);
				LabelInfo to = codegen.CurrentMethodDef.AddLabel ((int) yyVals[0+yyTop]);

				yyVal = new HandlerBlock (from, to);
			  }

void case_480()
#line 2397 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (
                                        new SimpInstr ((Op) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_481()
#line 2402 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (
                                        new IntInstr ((IntOp) yyVals[-1+yyTop], (int) yyVals[0+yyTop], tokenizer.Location));        
                          }

void case_482()
#line 2407 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                int slot = codegen.CurrentMethodDef.GetNamedLocalSlot ((string) yyVals[0+yyTop]);
                                if (slot < 0)
                                        Report.Error (String.Format ("Undeclared identifier '{0}'", (string) yyVals[0+yyTop]));
                                codegen.CurrentMethodDef.AddInstr (
                                        new IntInstr ((IntOp) yyVals[-1+yyTop], slot, tokenizer.Location));
                          }

void case_483()
#line 2415 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (
                                        new IntInstr ((IntOp) yyVals[-1+yyTop], (int) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_484()
#line 2420 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                int pos = codegen.CurrentMethodDef.GetNamedParamPos ((string) yyVals[0+yyTop]);
                                if (pos < 0)
                                        Report.Error (String.Format ("Undeclared identifier '{0}'", (string) yyVals[0+yyTop]));

                                codegen.CurrentMethodDef.AddInstr (
                                        new IntInstr ((IntOp) yyVals[-1+yyTop], pos, tokenizer.Location));
                          }

void case_485()
#line 2429 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (new
                                        IntInstr ((IntOp) yyVals[-1+yyTop], (int) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_486()
#line 2434 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                int slot = codegen.CurrentMethodDef.GetNamedLocalSlot ((string) yyVals[0+yyTop]);
                                if (slot < 0)
                                        Report.Error (String.Format ("Undeclared identifier '{0}'", (string) yyVals[0+yyTop]));
                                codegen.CurrentMethodDef.AddInstr (new
                                        IntInstr ((IntOp) yyVals[-1+yyTop], slot, tokenizer.Location));
                          }

void case_487()
#line 2442 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (yyVals[-1+yyTop] is MiscInstr) {
                                        switch ((MiscInstr) yyVals[-1+yyTop]) {
                                        case MiscInstr.ldc_i8:
                                        codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop],
                                                (long) yyVals[0+yyTop], tokenizer.Location));
                                        break;
                                        }
                                }
                          }

void case_488()
#line 2453 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                switch ((MiscInstr) yyVals[-1+yyTop]) {
                                case MiscInstr.ldc_r4:
                                case MiscInstr.ldc_r8:
                                         codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop], (double) yyVals[0+yyTop], tokenizer.Location));
                                         break;
                                }
                          }

void case_489()
#line 2462 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                long l = (long) yyVals[0+yyTop];
                                
                                switch ((MiscInstr) yyVals[-1+yyTop]) {
                                        case MiscInstr.ldc_r4:
                                        case MiscInstr.ldc_r8:
                                        codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop], (double) l, tokenizer.Location));
                                        break;
                                }
                          }

void case_490()
#line 2473 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				byte[] fpdata;
                                switch ((MiscInstr) yyVals[-1+yyTop]) {
                                        case MiscInstr.ldc_r4:
						fpdata = (byte []) yyVals[0+yyTop];
						if (!BitConverter.IsLittleEndian) {
							System.Array.Reverse (fpdata, 0, 4);
						}
                                                float s = BitConverter.ToSingle (fpdata, 0);
                                                codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop], s, tokenizer.Location));
                                                break;
                                        case MiscInstr.ldc_r8:
						fpdata = (byte []) yyVals[0+yyTop];
						if (!BitConverter.IsLittleEndian) {
							System.Array.Reverse (fpdata, 0, 8);
						}
                                                double d = BitConverter.ToDouble (fpdata, 0);
                                                codegen.CurrentMethodDef.AddInstr (new LdcInstr ((MiscInstr) yyVals[-1+yyTop], d, tokenizer.Location));
                                                break;
                                }
                          }

void case_491()
#line 2495 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo target = codegen.CurrentMethodDef.AddLabel ((int) yyVals[0+yyTop]);
                                codegen.CurrentMethodDef.AddInstr (new BranchInstr ((BranchOp) yyVals[-1+yyTop],
								   target, tokenizer.Location));  
                          }

void case_492()
#line 2501 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				LabelInfo target = codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]);
                                codegen.CurrentMethodDef.AddInstr (new BranchInstr ((BranchOp) yyVals[-1+yyTop],
                                        			   target, tokenizer.Location));
                          }

void case_493()
#line 2507 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (new MethodInstr ((MethodOp) yyVals[-1+yyTop],
                                        (BaseMethodRef) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_494()
#line 2512 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-2+yyTop];
                                GenericParamRef gpr = yyVals[-3+yyTop] as GenericParamRef;
                                if (gpr != null && codegen.CurrentMethodDef != null)
                                        codegen.CurrentMethodDef.ResolveGenParam ((PEAPI.GenParam) gpr.PeapiType);
                                IFieldRef fieldref = owner.GetFieldRef (
                                        (BaseTypeRef) yyVals[-3+yyTop], (string) yyVals[0+yyTop]);

                                codegen.CurrentMethodDef.AddInstr (new FieldInstr ((FieldOp) yyVals[-4+yyTop], fieldref, tokenizer.Location));
                          }

void case_495()
#line 2524 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                GlobalFieldRef fieldref = codegen.GetGlobalFieldRef ((BaseTypeRef) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);

                                codegen.CurrentMethodDef.AddInstr (new FieldInstr ((FieldOp) yyVals[-2+yyTop], fieldref, tokenizer.Location));
                          }

void case_496()
#line 2530 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentMethodDef.AddInstr (new TypeInstr ((TypeOp) yyVals[-1+yyTop],
                                        (BaseTypeRef) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_497()
#line 2535 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if ((MiscInstr) yyVals[-1+yyTop] == MiscInstr.ldstr)
                                        codegen.CurrentMethodDef.AddInstr (new LdstrInstr ((string) yyVals[0+yyTop], tokenizer.Location));
                          }

void case_498()
#line 2540 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                byte[] bs = (byte[]) yyVals[0+yyTop];
                                if ((MiscInstr) yyVals[-3+yyTop] == MiscInstr.ldstr)
                                        codegen.CurrentMethodDef.AddInstr (new LdstrInstr (bs, tokenizer.Location));
                          }

void case_499()
#line 2546 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                byte[] bs = (byte[]) yyVals[0+yyTop];
                                if ((MiscInstr) yyVals[-2+yyTop] == MiscInstr.ldstr)
                                        codegen.CurrentMethodDef.AddInstr (new LdstrInstr (bs, tokenizer.Location));
                          }

void case_500()
#line 2552 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                BaseTypeRef[] arg_array = null;

                                if (arg_list != null)
                                        arg_array = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));

                                codegen.CurrentMethodDef.AddInstr (new CalliInstr ((CallConv) yyVals[-4+yyTop],
                                        (BaseTypeRef) yyVals[-3+yyTop], arg_array, tokenizer.Location));
                          }

void case_501()
#line 2563 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if ((MiscInstr) yyVals[-1+yyTop] == MiscInstr.ldtoken) {
                                        if (yyVals[0+yyTop] is BaseMethodRef)
                                                codegen.CurrentMethodDef.AddInstr (new LdtokenInstr ((BaseMethodRef) yyVals[0+yyTop], tokenizer.Location));
                                        else if (yyVals[0+yyTop] is IFieldRef)
                                                codegen.CurrentMethodDef.AddInstr (new LdtokenInstr ((IFieldRef) yyVals[0+yyTop], tokenizer.Location));
                                        else
                                                codegen.CurrentMethodDef.AddInstr (new LdtokenInstr ((BaseTypeRef) yyVals[0+yyTop], tokenizer.Location));
                                                
                                }
                          }

void case_503()
#line 2582 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                GenericArguments ga = (GenericArguments) yyVals[-3+yyTop];
                                BaseTypeRef[] param_list;
  
                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

				BaseMethodRef methref = codegen.GetGlobalMethodRef ((BaseTypeRef) yyVals[-5+yyTop], (CallConv) yyVals[-6+yyTop],
                                        (string) yyVals[-4+yyTop], param_list, (ga != null ? ga.Count : 0));

                                if (ga != null)
                                        methref = methref.GetGenericMethodRef (ga);

                                yyVal = methref;
                          }

void case_504()
#line 2602 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-6+yyTop];
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                GenericArguments ga = (GenericArguments) yyVals[-3+yyTop];
                                BaseTypeRef[] param_list;
                                BaseMethodRef methref;

                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                if (codegen.IsThisAssembly ("mscorlib")) {
                                        PrimitiveTypeRef prim = owner as PrimitiveTypeRef;
                                        if (prim != null && prim.SigMod == "")
                                                owner = codegen.GetTypeRef (prim.Name);
                                }

                                if (owner.UseTypeSpec) {
                                        methref = new TypeSpecMethodRef (owner, (CallConv) yyVals[-8+yyTop], (BaseTypeRef) yyVals[-7+yyTop],
                                                (string) yyVals[-4+yyTop], param_list, (ga != null ? ga.Count : 0));
                                } else {
                                        methref = owner.GetMethodRef ((BaseTypeRef) yyVals[-7+yyTop],
                                                (CallConv) yyVals[-8+yyTop], (string) yyVals[-4+yyTop], param_list, (ga != null ? ga.Count : 0));
                                }

                                if (ga != null)
                                        methref = methref.GetGenericMethodRef (ga);
                                
                                yyVal = methref;
                          }

void case_506()
#line 2637 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList label_list = new ArrayList ();
                                label_list.Add (codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]));
                                yyVal = label_list;
                          }

void case_507()
#line 2643 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList label_list = new ArrayList ();
                                label_list.Add (yyVals[0+yyTop]);
                                yyVal = label_list;
                          }

void case_508()
#line 2649 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList label_list = (ArrayList) yyVals[-2+yyTop];
                                label_list.Add (codegen.CurrentMethodDef.AddLabelRef ((string) yyVals[0+yyTop]));
                          }

void case_509()
#line 2654 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList label_list = (ArrayList) yyVals[-2+yyTop];
                                label_list.Add (yyVals[0+yyTop]);
                          }

void case_513()
#line 2669 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-2+yyTop];

                                yyVal = owner.GetFieldRef (
                                        (BaseTypeRef) yyVals[-3+yyTop], (string) yyVals[0+yyTop]);
                          }

void case_516()
#line 2688 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                EventDef event_def = new EventDef ((FeatureAttr) yyVals[-2+yyTop],
                                        (BaseTypeRef) yyVals[-1+yyTop], (string) yyVals[0+yyTop]);
                                codegen.CurrentTypeDef.BeginEventDef (event_def);
                                codegen.CurrentCustomAttrTarget = event_def;
                          }

void case_523()
#line 2716 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.CurrentEvent.AddAddon (
                                        (MethodRef) yyVals[-1+yyTop]);                                
                          }

void case_524()
#line 2721 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.CurrentEvent.AddRemoveon (
                                        (MethodRef) yyVals[-1+yyTop]);
                          }

void case_525()
#line 2726 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.CurrentEvent.AddFire (
                                        (MethodRef) yyVals[-1+yyTop]);
                          }

void case_526()
#line 2731 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.CurrentTypeDef.CurrentEvent.AddOther (
                                        (MethodRef) yyVals[-1+yyTop]);
                          }

void case_527()
#line 2736 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                          }

void case_531()
#line 2751 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                PropertyDef prop_def = new PropertyDef ((FeatureAttr) yyVals[-6+yyTop], (BaseTypeRef) yyVals[-5+yyTop],
                                        (string) yyVals[-4+yyTop], (ArrayList) yyVals[-2+yyTop]);
                                codegen.CurrentTypeDef.BeginPropertyDef (prop_def);
                                codegen.CurrentCustomAttrTarget = prop_def;

                                if (yyVals[0+yyTop] != null) {
                                        prop_def.AddInitValue ((Constant) yyVals[0+yyTop]);
                                }
                          }

void case_541()
#line 2798 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                         }

void case_551()
#line 2831 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
			  		var l = (List<BoolConst>) yyVals[-1+yyTop];
			  		yyVal = new ArrayConstant (l?.ToArray ()) {
			  			ExplicitSize = (int) yyVals[-4+yyTop]
			  		};
				  }

void case_554()
#line 2846 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				  	var c = yyVals[0+yyTop] as Constant;
			  		yyVal = c ?? new ArrayConstant (((List<DataConstant>) yyVals[0+yyTop]).ToArray ());
				  }

void case_556()
#line 2855 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				  	var l = yyVals[-1+yyTop] as List<DataConstant>;
				  	if (l == null) {
				  		l = new List<DataConstant> () {
				  			(DataConstant) yyVals[-1+yyTop]
				  		};
				  	}

				  	l.Add ((DataConstant) yyVals[0+yyTop]);
				  	yyVal = l;
				  }

void case_559()
#line 2878 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                BaseTypeRef owner = (BaseTypeRef) yyVals[-5+yyTop];
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                BaseTypeRef[] param_list;
  
                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                yyVal = owner.GetMethodRef ((BaseTypeRef) yyVals[-6+yyTop],
                                        (CallConv) yyVals[-7+yyTop], (string) yyVals[-3+yyTop], param_list, 0);
                          }

void case_560()
#line 2892 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList arg_list = (ArrayList) yyVals[-1+yyTop];
                                BaseTypeRef[] param_list;
  
                                if (arg_list != null)
                                        param_list = (BaseTypeRef[]) arg_list.ToArray (typeof (BaseTypeRef));
                                else
                                        param_list = new BaseTypeRef[0];

                                yyVal = codegen.GetGlobalMethodRef ((BaseTypeRef) yyVals[-4+yyTop], (CallConv) yyVals[-5+yyTop],
                                        (string) yyVals[-3+yyTop], param_list, 0);
                          }

void case_563()
#line 2915 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				System.Text.UnicodeEncoding ue = new System.Text.UnicodeEncoding ();
				PermissionSetAttribute psa = new PermissionSetAttribute ((System.Security.Permissions.SecurityAction) (short) yyVals[-2+yyTop]);
				psa.XML = ue.GetString ((byte []) yyVals[0+yyTop]);
				yyVal = new PermPair ((PEAPI.SecurityAction) yyVals[-2+yyTop], psa.CreatePermissionSet ());
			  }

void case_564()
#line 2922 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				PermissionSetAttribute psa = new PermissionSetAttribute ((System.Security.Permissions.SecurityAction) (short) yyVals[-1+yyTop]);
				psa.XML = (string) yyVals[0+yyTop];
				yyVal = new PermPair ((PEAPI.SecurityAction) yyVals[-1+yyTop], psa.CreatePermissionSet ());
			  }

void case_566()
#line 2934 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				ArrayList list = new ArrayList ();
				list.Add (yyVals[0+yyTop]);
				yyVal = list;
			  }

void case_567()
#line 2940 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				ArrayList list = (ArrayList) yyVals[-2+yyTop];
				list.Add (yyVals[0+yyTop]);
				yyVal = list;
			  }

void case_569()
#line 2954 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				  ArrayList list = new ArrayList ();
				  list.Add (yyVals[0+yyTop]);
				  yyVal = list;
			  }

void case_570()
#line 2960 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				  ArrayList list = (ArrayList) yyVals[-1+yyTop];
				  list.Add (yyVals[0+yyTop]);
				  yyVal = list;
			  }

void case_571()
#line 2968 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				NameValuePair pair = (NameValuePair) yyVals[0+yyTop];
				yyVal = new PermissionMember ((MemberTypes) yyVals[-2+yyTop], (BaseTypeRef) yyVals[-1+yyTop], pair.Name, pair.Value);
			  }

void case_572()
#line 2973 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				NameValuePair pair = (NameValuePair) yyVals[0+yyTop];
				yyVal = new PermissionMember ((MemberTypes) yyVals[-3+yyTop], (BaseTypeRef) yyVals[-1+yyTop], pair.Name, pair.Value);
			  }

void case_576()
#line 2996 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				ArrayList pairs = new ArrayList ();
				pairs.Add (yyVals[0+yyTop]);
				yyVal = pairs;
			  }

void case_577()
#line 3002 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
			  	ArrayList pairs = (ArrayList) yyVals[-2+yyTop];
				pairs.Add (yyVals[0+yyTop]);
				yyVal = pairs;
			  }

void case_606()
#line 3122 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                /* We need to compute the hash ourselves. :-(*/
                                /* AssemblyName an = AssemblyName.GetName ((string) $3);*/
                          }

void case_611()
#line 3149 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				codegen.CurrentCustomAttrTarget = null;
				codegen.CurrentDeclSecurityTarget = null;
			  }

void case_612()
#line 3156 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                codegen.SetThisAssembly ((string) yyVals[0+yyTop], (PEAPI.AssemAttr) yyVals[-1+yyTop]);
                                codegen.CurrentCustomAttrTarget = codegen.ThisAssembly;
				codegen.CurrentDeclSecurityTarget = codegen.ThisAssembly;
                          }

void case_630()
#line 3218 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                System.Reflection.AssemblyName asmb_name = 
					new System.Reflection.AssemblyName ();
				asmb_name.Name = (string) yyVals[0+yyTop];
				codegen.BeginAssemblyRef ((string) yyVals[0+yyTop], asmb_name, (PEAPI.AssemAttr) yyVals[-1+yyTop]);
                          }

void case_631()
#line 3225 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                System.Reflection.AssemblyName asmb_name = 
					new System.Reflection.AssemblyName ();
				asmb_name.Name = (string) yyVals[-2+yyTop];
				codegen.BeginAssemblyRef ((string) yyVals[0+yyTop], asmb_name, (PEAPI.AssemAttr) yyVals[-3+yyTop]);
                          }

void case_640()
#line 3260 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                if (codegen.CurrentCustomAttrTarget != null)
                                        codegen.CurrentCustomAttrTarget.AddCustomAttribute ((CustomAttr) yyVals[0+yyTop]);
                          }

void case_661()
#line 3305 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
				FileStream s = new FileStream ((string) yyVals[0+yyTop], FileMode.Open, FileAccess.Read);
				byte [] buff = new byte [s.Length];
				s.Read (buff, 0, (int) s.Length);
				s.Close ();

				codegen.AddManifestResource (new ManifestResource ((string) yyVals[0+yyTop], buff, (yyVals[-1+yyTop] == null) ? 0 : (uint) yyVals[-1+yyTop]));
			  }

void case_672()
#line 3334 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                long l = (long) yyVals[0+yyTop];
                                byte[] intb = BitConverter.GetBytes (l);
                                yyVal = BitConverter.ToInt32 (intb, BitConverter.IsLittleEndian ? 0 : 4);
                          }

void case_675()
#line 3346 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                int i = (int) yyVals[-1+yyTop];
                                byte[] intb = BitConverter.GetBytes (i);
                                yyVal = (double) BitConverter.ToSingle (intb, 0);
                          }

void case_676()
#line 3352 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                long l = (long) yyVals[-1+yyTop];
                                byte[] intb = BitConverter.GetBytes (l);
                                yyVal = (double) BitConverter.ToSingle (intb, BitConverter.IsLittleEndian ? 0 : 4);
                          }

void case_677()
#line 3358 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                byte[] intb = BitConverter.GetBytes ((long) yyVals[-1+yyTop]);
				yyVal = BitConverter.ToDouble (intb, 0);
                          }

void case_678()
#line 3363 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                byte[] intb = BitConverter.GetBytes ((int) yyVals[-1+yyTop]);
                                yyVal = (double) BitConverter.ToSingle (intb, 0);
                          }

void case_681()
#line 3377 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                yyVal = yyVals[-1+yyTop];
                                tokenizer.InByteArray = false;
                          }

void case_683()
#line 3385 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList byte_list = (ArrayList) yyVals[0+yyTop];
                                yyVal = byte_list.ToArray (typeof (byte));
                          }

void case_684()
#line 3392 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList byte_list = new ArrayList ();
                                byte_list.Add (Convert.ToByte (yyVals[0+yyTop]));
                                yyVal = byte_list;
                          }

void case_685()
#line 3398 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"
{
                                ArrayList byte_list = (ArrayList) yyVals[-1+yyTop];
                                byte_list.Add (Convert.ToByte (yyVals[0+yyTop]));
                          }

#line default
   static readonly short [] yyLhs  = {              -1,
    0,    1,    1,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,   19,   19,   19,   19,   20,   20,
   20,    8,   21,   21,   21,   21,   21,    4,   23,    3,
   25,   27,   27,   27,   27,   27,   27,   27,   27,   27,
   27,   27,   27,   27,   27,   27,   27,   27,   27,   27,
   27,   27,   27,   27,   27,   27,   29,   29,   30,   30,
   32,   32,   28,   28,   34,   34,   35,   35,   37,   37,
   38,   38,   31,   31,   31,   31,   31,   31,   31,   33,
   33,   40,   40,   40,   40,   40,   40,   41,   42,   42,
   43,   43,   44,   44,   39,   39,   39,   26,   26,   45,
   45,   45,   45,   45,   45,   45,   45,   45,   45,   45,
   45,   52,   45,   45,   36,   36,   36,   36,   36,   36,
   36,   36,   36,   36,   36,   36,   36,   56,   56,   56,
   56,   56,   56,   56,   56,   56,   56,   56,   56,   56,
   56,   56,   56,   56,   56,   56,   56,   56,   56,   56,
   54,   54,   57,   57,   57,   57,   57,   50,   50,   50,
   58,   58,   58,   58,   58,   58,   58,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   59,
   59,   59,   59,   59,   59,   59,   59,   59,   59,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   61,   61,   61,   61,   61,   61,   61,
   61,   61,   61,   55,   55,    6,   62,   62,   63,   63,
   63,   63,   63,   63,   63,   63,   63,   63,   63,   63,
   63,   63,   63,   64,   64,   65,   65,   68,   68,   68,
   68,   68,   68,   68,   68,   68,   68,   68,   68,   68,
   68,   68,   71,   71,   67,   67,   67,   73,   73,   74,
   75,   75,    7,   76,   76,   78,   78,   77,   77,   79,
   79,   80,   80,   80,   80,   80,   80,   80,   80,   80,
   80,   80,   80,   80,   80,   80,   80,   80,    5,   81,
   81,   83,   83,   83,   83,   83,   83,   83,   83,   83,
   83,   83,   83,   83,   83,   83,   83,   83,   83,   83,
   83,   83,   83,   83,   86,   86,   86,   86,   86,   86,
   86,   86,   86,   86,   86,   86,   86,   86,   86,   49,
   49,   49,   84,   84,   84,   84,   85,   85,   85,   85,
   85,   85,   85,   85,   85,   85,   85,   85,   85,   53,
   53,   87,   87,   88,   88,   88,   88,   88,   51,   51,
   51,   51,   51,   89,   89,   82,   82,   90,   90,   90,
   90,   90,   90,   90,   90,   90,   90,   90,   90,   90,
   90,   90,   90,   90,   90,   90,   90,   90,   90,   90,
   91,   91,   91,   96,   96,   96,   96,   97,   48,   48,
   48,   93,   98,   94,   99,   99,   99,  100,  100,  101,
  101,  101,  101,  103,  103,  103,  102,  102,  102,   95,
   95,   95,   95,   95,   95,   95,   95,   95,   95,   95,
   95,   95,   95,   95,   95,   95,   95,   95,   95,   95,
   95,   95,   92,   92,  105,  105,  105,  105,  105,  104,
  104,  106,  106,  106,   46,  107,  107,  109,  109,  109,
  108,  108,  110,  110,  110,  110,  110,  110,  110,   47,
  111,  113,  113,  113,  113,  112,  112,  114,  114,  114,
  114,  114,  114,   16,   16,   16,   16,  115,  115,  117,
  117,  117,  117,  117,  118,  118,  119,  119,  116,  116,
   15,   15,   15,   15,   15,  122,  122,  123,  124,  124,
  125,  125,  127,  126,  126,  121,  121,  128,  129,  129,
  129,  129,  129,  129,  129,  129,  120,  120,  120,  120,
  120,  120,  120,  120,  120,  120,  120,  120,  120,  120,
  120,   14,   14,   14,    9,    9,  130,  130,  131,  131,
   10,  132,  135,  135,  133,  133,  136,  136,  136,  136,
  136,  136,  136,  137,  137,  137,  137,  137,   11,  138,
  138,  139,  139,  140,  140,  140,  140,  140,  140,  140,
  140,   12,  141,  143,  143,  143,  143,  143,  143,  143,
  143,  143,  143,  142,  142,  144,  144,  144,  144,   13,
  145,  147,  147,  147,  146,  146,  148,  148,  148,   60,
   60,   17,   18,   69,   69,   69,   69,   69,  149,  151,
   72,  150,  150,  152,  152,   70,   70,   22,   22,   24,
   24,   24,   66,   66,  134,  134,
  };
   static readonly short [] yyLen = {           2,
    1,    0,    2,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    2,    2,    3,
    2,    2,    1,    1,    3,    2,    5,    4,    2,    4,
    6,    7,    0,    2,    2,    2,    2,    4,    2,    4,
    6,    0,    2,    2,    3,    3,    3,    3,    3,    3,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    0,    2,    0,    1,
    2,    3,    0,    3,    0,    3,    1,    3,    0,    3,
    1,    3,    1,    1,    3,    2,    3,    2,    3,    3,
    5,    0,    2,    2,    2,    2,    2,    1,    3,    5,
    1,    3,    1,    3,    4,    5,    1,    0,    2,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    2,
    2,    0,   15,    1,    1,    3,    6,    3,    3,    4,
    2,    2,    2,    5,    5,    7,    1,    1,    1,    1,
    1,    1,    1,    2,    1,    2,    1,    2,    1,    2,
    1,    2,    3,    2,    1,    1,    1,    1,    1,    1,
    1,    3,    0,    1,    1,    3,    2,    2,    2,    1,
    0,    1,    1,    2,    2,    2,    2,    0,    6,    5,
    5,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    2,    1,    2,    1,    2,    1,    2,
    1,    2,    3,    4,    6,    5,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    2,    4,    1,
    2,    2,    1,    2,    1,    2,    1,    2,    1,    0,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    2,    2,    2,    2,    1,    3,    2,    2,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    2,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    8,    0,    3,    0,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    5,    2,    2,    0,    2,    0,    2,    4,    4,    4,
    4,    4,    4,    4,    4,    4,    4,    4,    4,    4,
    4,    4,    1,    2,    1,    1,    1,    1,    4,    1,
    1,    2,    2,    4,    2,    0,    1,    3,    1,    1,
    3,    5,    5,    4,    3,    2,    5,    5,    5,    5,
    5,    5,    2,    2,    2,    2,    2,    2,    4,   11,
   14,    0,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    8,    6,    5,    0,    2,    2,    2,    2,    2,
    2,    2,    2,    2,    2,    4,    4,    4,    4,    1,
    1,    1,    0,    4,    4,    4,    0,    2,    2,    2,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    0,
    1,    1,    3,    2,    3,    1,    6,    7,    0,    1,
    3,    3,    5,    0,    1,    0,    2,    2,    2,    4,
    5,    1,    1,    4,    6,    4,    4,    3,   15,    1,
    5,    1,    2,    1,    1,    1,    1,    1,    1,    1,
    0,    1,    3,    1,    2,    2,    3,    3,    3,    4,
    1,    3,    1,    2,    2,    4,    4,    1,    2,    3,
    2,    2,    2,    2,    2,    2,    1,    4,    4,    1,
    2,    2,    2,    2,    2,    2,    2,    2,    2,    2,
    2,    2,    2,    5,    3,    2,    2,    4,    3,    6,
    2,    4,    7,    9,    0,    1,    1,    3,    3,    1,
    1,    2,    5,    3,    4,    4,    3,    0,    2,    2,
    0,    2,    3,    3,    3,    3,    1,    1,    1,    4,
    8,    0,    2,    2,    2,    0,    2,    2,    2,    2,
    1,    1,    1,    3,    5,    5,    7,    0,    3,    0,
    7,    2,    4,    1,    1,    2,    1,    4,    8,    6,
    6,    3,    4,    3,    6,    1,    3,    5,    1,    2,
    3,    4,    3,    1,    1,    1,    3,    3,    1,    1,
    4,    1,    6,    6,    6,    4,    1,    1,    1,    1,
    1,    1,    1,    1,    1,    1,    1,    1,    1,    1,
    1,    1,    2,    3,    8,    4,    0,    2,    0,    1,
    4,    4,    0,    2,    0,    2,    3,    8,    2,    3,
    3,    1,    1,    3,    8,    2,    3,    1,    4,    5,
    7,    0,    2,    8,    3,    3,    2,    3,    3,    1,
    1,    4,    4,    0,    2,    2,    2,    3,    3,    3,
    3,    3,    3,    0,    2,    2,    3,    1,    3,    4,
    3,    0,    2,    2,    0,    2,    4,    3,    1,    1,
    3,    1,    1,    1,    4,    4,    4,    4,    1,    0,
    4,    0,    1,    1,    2,    1,    1,    1,    1,    1,
    3,    1,    0,    1,    0,    2,
  };
   static readonly short [] yyDefRed = {            2,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  352,    0,  662,    0,    0,    0,    0,    0,
    0,    3,    4,    5,    6,    7,    8,    9,   10,   11,
   12,   13,   14,   15,   16,   17,   23,   24,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  613,  644,
    0,  673,   21,  672,   19,    0,    0,  327,    0,    0,
  279,    0,    0,    0,    0,    0,  688,  689,  692,    0,
  690,    0,    0,    0,  587,  588,  589,  590,  591,  592,
  593,  594,  595,  596,  597,  598,  599,  600,  601,    0,
    0,   22,   18,    0,    2,  108,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  323,  329,  426,
  615,  632,  654,  665,  613,  696,    0,    0,   58,   43,
   44,   66,   65,   51,   53,   54,   55,   56,   57,   59,
   60,   61,   62,   63,    0,   64,   52,    0,    0,    0,
    0,    0,  158,  159,  138,  139,  140,  141,  142,  143,
    0,  145,  147,  149,  151,    0,    0,  155,  157,  156,
    0,   84,  160,    0,  125,    0,   83,    0,  137,    0,
    0,  172,  173,    0,    0,  170,    0,    0,    0,    0,
   20,  608,    0,    0,   25,    0,  353,  354,  355,  356,
  368,  369,  367,  357,  358,  359,  360,  363,  361,  362,
  364,  365,  371,    0,  370,  366,  393,    0,    0,  663,
  664,    0,    0,    0,    0,  670,    0,    0,    0,    0,
    0,    0,  330,    0,    0,  348,    0,  347,    0,  346,
    0,  345,    0,  343,    0,  344,    0,    0,  680,    0,
  336,    0,    0,    0,    0,    0,    0,  614,    0,  646,
  645,    0,  647,    0,   46,   45,   47,   48,   49,   50,
   92,    0,    0,    0,    0,   86,   88,    0,    0,  154,
  152,  144,  146,  148,  150,    0,    0,    0,    0,    0,
  549,  132,  131,  133,    0,    0,    0,  168,  169,  174,
  175,  176,  177,    0,    0,  324,  278,    0,  287,  280,
  281,  282,  288,  289,  290,  283,  284,  285,  286,  292,
  293,    0,  610,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  563,    0,   33,   38,   40,   42,  518,
    0,    0,    0,  532,    0,  111,  110,  114,  115,  116,
  118,  117,  124,  119,  109,  112,  113,    0,    0,  328,
    0,    0,    0,    0,    0,    0,  674,    0,    0,    0,
    0,    0,    0,    0,  335,  463,  349,  480,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  432,    0,    0,    0,    0,    0,    0,    0,
  433,  450,  446,  449,  447,  448,    0,  442,  427,  440,
  444,  445,  426,    0,  611,    0,    0,    0,    0,  623,
  622,  616,  629,    0,    0,    0,    0,    0,  641,  640,
  633,  642,    0,    0,    0,  658,  655,  660,    0,    0,
  669,  666,    0,  648,  649,  650,  651,  652,  653,    0,
    0,    0,    0,    0,    0,   87,   89,  126,  153,    0,
    0,   85,    0,  128,  129,  164,    0,    0,  161,    0,
    0,    0,    0,  391,  390,    0,    0,    0,    0,    0,
  546,    0,    0,    0,    0,   27,    0,    0,    0,    0,
    0,    0,    0,    0,  576,    0,    0,  566,  671,    0,
    0,    0,  121,    0,    0,  120,  521,  536,  331,  334,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  679,  684,    0,    0,  485,  486,  487,  489,  488,  490,
  491,  492,    0,  493,    0,  496,    0,    0,    0,    0,
    0,  510,  501,  511,    0,  481,  482,  483,  484,  428,
    0,    0,    0,  429,    0,    0,    0,    0,    0,  465,
    0,  443,    0,    0,    0,    0,    0,    0,  468,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,   92,   74,    0,
   93,   94,   95,   97,   96,    0,   68,    0,   41,    0,
    0,    0,    0,    0,    0,    0,    0,  130,    0,  275,
    0,  274,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  557,    0,    0,  555,    0,  217,    0,    0,    0,
    0,    0,  183,  184,  185,  186,  187,  188,  189,  190,
  191,  192,  193,    0,  195,  197,  199,  201,  207,  208,
  209,  210,  211,  212,  213,  214,  215,  216,    0,  220,
  223,  225,  229,  227,    0,    0,    0,    0,   31,    0,
    0,  374,  382,  383,  384,  385,  377,  378,  379,    0,
  376,  380,  381,    0,    0,    0,    0,    0,    0,    0,
    0,    0,  561,    0,    0,  565,    0,    0,   34,   35,
   36,   37,  519,  520,    0,    0,    0,    0,   99,  535,
  533,  534,    0,    0,    0,  342,  341,  340,  339,    0,
    0,    0,    0,  337,  338,  333,  332,  681,  685,    0,
    0,    0,    0,  499,    0,    0,  512,    0,  507,  506,
    0,    0,    0,    0,    0,  452,    0,    0,    0,  438,
    0,    0,    0,    0,    0,  462,    0,  476,  475,  474,
    0,  477,  471,  472,  469,  473,  621,  620,  617,    0,
  639,  638,  635,  636,    0,    0,    0,    0,    0,    0,
    0,    0,    0,   98,   90,   71,    0,    0,    0,    0,
   76,    0,  166,  162,  134,  135,    0,  420,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  552,    0,  547,    0,  556,
  228,  224,  222,    0,    0,    0,  226,  194,  196,  198,
  200,  221,  246,  232,  233,  234,  235,  236,  237,  238,
  239,  240,  241,  260,    0,  250,  251,  252,  253,  254,
  255,  256,  257,  258,  231,  261,  262,  263,  264,  265,
  266,  267,  268,  269,  270,  271,  272,  273,    0,    0,
  291,  202,  295,    0,    0,    0,    0,  373,    0,    0,
  394,  395,  396,    0,    0,  686,  687,    0,    0,    0,
  579,  578,  577,    0,  567,   32,    0,    0,    0,    0,
  515,    0,    0,    0,    0,  527,  528,  529,  522,  530,
    0,    0,    0,  541,  542,  543,  537,  675,  676,  678,
  677,    0,    0,    0,  498,    0,    0,    0,    0,  502,
    0,    0,    0,  455,  430,    0,    0,    0,    0,  437,
    0,  467,  466,  436,  470,    0,    0,    0,    0,  667,
    0,   80,    0,   72,  416,    0,    0,    0,  412,    0,
  127,    0,  560,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  242,  243,  244,  245,
  259,    0,    0,  249,  248,  203,    0,    0,    0,  317,
    0,  297,  313,  315,  694,  276,  605,    0,  386,  387,
  388,  389,    0,    0,    0,    0,  575,  574,    0,  569,
    0,    0,  100,    0,    0,    0,    0,    0,  539,  540,
  538,    0,    0,  494,    0,    0,  509,  508,    0,  458,
  453,  457,  431,    0,  441,    0,    0,    0,    0,   91,
    0,  136,    0,    0,    0,  421,    0,  425,  422,    0,
  312,  310,  306,  304,  302,  300,  298,  301,  299,  311,
  307,  305,  303,  558,  309,  308,  553,    0,    0,    0,
  247,    0,    0,  204,    0,  314,  372,    0,    0,    0,
    0,    0,    0,    0,  568,  570,    0,    0,    0,    0,
  523,  525,  526,  524,    0,    0,  500,  513,  435,    0,
  479,  478,    0,    0,    0,  415,  413,  559,    0,    0,
    0,  180,  181,  206,    0,    0,  397,  581,    0,    0,
    0,  586,    0,    0,  571,    0,    0,    0,    0,    0,
    0,    0,    0,  423,  321,    0,    0,  179,  205,    0,
    0,    0,    0,    0,  572,    0,    0,  531,    0,  503,
    0,  618,  634,    0,  551,  322,    0,  403,  398,  400,
  399,  401,  402,  404,  406,  407,  408,  409,  405,  583,
  584,  585,    0,  318,  573,    0,    0,    0,    0,  397,
    0,    0,  504,    0,  418,    0,    0,    0,    0,  319,
    0,    0,    0,    0,    0,    0,  123,  439,
  };
  protected static readonly short [] yyDgoto  = {             1,
    2,   22,   23,   24,   25,   26,   27,   28,   29,   30,
   31,   32,   33,   34,   35,   36,  457,   53,   37,   38,
  490,   71,   39,  164,   40,  221,   51,  262,  443,  589,
  165,  590,  440, 1138,  594,  214,  586,  783,  167,  441,
  785,  398,    0,  168,  345,  346,  347,  922,  923,  523,
  799, 1201,  956,  458,  601,  169,  459,  176,  665,  483,
  869,   61,  180,  667,  875, 1006, 1002,  622,  360,  891,
 1004,  241, 1185, 1146, 1147,   41,  108,   59,  222,  109,
   42,  242,   66,  800, 1151,  478,  958,  959, 1059,  399,
  745,  524,  762,  401,  402,  746,  747,  403,  404,  558,
  559,  763,  560,  533,  741,  534,  348,  714,  491,  909,
  349,  715,  495,  917,   57,  177,  623,  624,  625,   90,
  484,  487,  488, 1019, 1020, 1021, 1135,  485,  892,   63,
  314,   43,  243,   49,  117,  412,    0,   44,  244,  421,
   45,  245,  118,  427,   46,  246,   73,  432,  512,  513,
  364,  514,
  };
  protected static readonly short [] yySindex = {            0,
    0, 5060, -306, -261, -133,  -79,  -72, -363,  -13, -265,
   40,  -79,    0, -116,    0,  273, 2688, 2688, -133,  -79,
   54,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,   85,  103,
  599,  120,  129,  147,  153,  167,  -98, -109,    0,    0,
 5037,    0,    0,    0,    0, 2156,  362,    0,  320,  -79,
    0,  -79, -220,  193, -170, 2567,    0,    0,    0,  273,
    0,  221,   93,  221,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 5366,
 -107,    0,    0,  -79,    0,    0,  440,  233,  351,  375,
  398,  427,  442,  446,  261,  267,  -56,    0,    0,    0,
    0,    0,    0,    0,    0,    0, -196, -191,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  408,    0,    0, -108,  -86, -123,
   73, -332,    0,    0,    0,    0,    0,    0,    0,    0,
  326,    0,    0,    0,    0,  362,  340,    0,    0,    0,
  424,    0,    0,  221,    0,  204,    0,  271,    0,  362,
  362,    0,    0,  407, 2156,    0,  287,  330,  314, 4697,
    0,    0, -192,  342,    0,  -79,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  369,    0,    0,    0,  221,  273,    0,
    0,  221,  111,  234,  388,    0, -154,  371,  433, 6861,
 5527,   41,    0,  320,  -79,    0,  -79,    0,  -79,    0,
 -133,    0, -187,    0, -187,    0,  437,  445,    0,  448,
    0, 6382,  930,  206,  763,   11, -196,    0,  271,    0,
    0,  727,    0,  221,    0,    0,    0,    0,    0,    0,
    0,  302,  273,  -62,  294,    0,    0,  340,  263,    0,
    0,    0,    0,    0,    0, 2156,  464,  273,  124, -165,
    0,    0,    0,    0,  485,  499,  273,    0,    0,    0,
    0,    0,    0, 4730,   68,    0,    0,  515,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  758,    0,  467,  537,  577,  581, 5442,  221,  273,
   91,  581,  340,    0,  592,    0,    0,    0,    0,    0,
 5366,  -79,  352,    0,  -79,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  586,  602,    0,
  440,  606,  608,  615,  625,  627,    0,  632,  636,  651,
  654,  581,  581,  663,    0,    0,    0,    0,  294, -133,
 -140,  294,  362, 2156, 5366, -206,  362, 5294,  671,  294,
  294,  -79,    0,  691, -216,  -79, 5506, -233,  553,  -79,
    0,    0,    0,    0,    0,    0,  689,    0,    0,    0,
    0,    0,    0,  309,    0,  432,  -74,  707,  -79,    0,
    0,    0,    0,  712,  -69,  736,  737,  -79,    0,    0,
    0,    0,  631,  642,  273,    0,    0,    0,  645,  273,
    0,    0, -223,    0,    0,    0,    0,    0,    0, -186,
  -83, -122,  600,  115,  273,    0,    0,    0,    0,  257,
 2156,    0,  190,    0,    0,    0,  739,  334,    0,  607,
  607,  221,  185,    0,    0,  221,  749,  754, 2177,  371,
    0, 6162,  656,  756,  772,    0, -246,  458,  640,  -44,
  219,  273,  332,  207,    0,  757,  142,    0,    0, -241,
 5086,  761,    0,  306, 5234,    0,    0,    0,    0,    0,
  -13,  -13,  -13,  -13,  235,  296,  -13,  -13, -119, -118,
    0,    0,  769,  663,    0,    0,    0,    0,    0,    0,
    0,    0, 2156,    0, 1092,    0,  -20,  371, 2156,  362,
 2156,    0,    0,    0,  294,    0,    0,    0,    0,    0,
  -79, 5522,  773,    0,  362,  770,  -79,  534,  552,    0,
  775,    0, 6469, 2156,  553, -199, -199,  309,    0, -199,
  -79,  448,  371,  448,  792,  448,  448,  371,  448,  448,
  797,  273,  273,  221,  273, -224,  273,    0,    0, 2156,
    0,    0,    0,    0,    0,  320,    0, -122,    0,  804,
  273,  271,  809,  -81,  234,  273,  -79,    0, -169,    0,
  810,    0,  811,  325,  805,  483,  820,  822,  823,  825,
  826,  828,  829,  831,  832,  835,  836,  840,  847,  448,
  851,    0,  813, 2238,    0,  650,    0,  665,  661,  867,
  301,  690,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  225,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 6035,    0,
    0,    0,    0,    0,   69,  320,  861,  448,    0,  581,
  702,    0,    0,    0,    0,    0,    0,    0,    0,  870,
    0,    0,    0,  873,  871,  878,  879,  890,  221,  863,
  273, -149,    0,  581,  895,    0,  340,  320,    0,    0,
    0,    0,    0,    0,    0,  273,  325,  -79,    0,    0,
    0,    0,  357, 2098, 1033,    0,    0,    0,    0,  894,
  898,  899,  904,    0,    0,    0,    0,    0,    0, 4730,
    0,  912,  448,    0,  231, 2156,    0, 1092,    0,    0,
  488,  920,  172,  758,  511,    0, 2156, 5522, 2156,    0,
  325,  923,  -79,  320,  -79,    0,  170,    0,    0,    0,
  294,    0,    0,    0,    0,    0,    0,    0,    0,  -79,
    0,    0,    0,    0,  -79,  221,  221,  271,  -79,  271,
  -83,  234,  546,    0,    0,    0, -122,  271,  910, 2156,
    0,  124,    0,    0,    0,    0,  925,    0,  576, 5442,
  -79,  307, -133, -133, -133, -133,  -14,  -14, -133, -133,
 -133, -133, 2156, -133, -133,    0,  937,    0,  931,    0,
    0,    0,    0,  581,  932,  942,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  275,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -177,  -70,
    0,    0,    0, -222,  938,  892,  371,    0,  318,  349,
    0,    0,    0, 6162,  947,    0,    0,  950,  953,  371,
    0,    0,    0,  343,    0,    0,  221,  722,  961,  199,
    0,  362,  362,  362,  362,    0,    0,    0,    0,    0,
  362,  362,  362,    0,    0,    0,    0,    0,    0,    0,
    0,  958,  464,  320,    0,  805, 5009,    0,  964,    0,
  294,  872,  972,    0,    0, 5522,  758,  637, 5009,    0,
  861,    0,    0,    0,    0,  732,  745,  974,  989,    0,
  320,    0, 2156,    0,    0,  992, 5442,  993,    0,  234,
    0,  805,    0,  981,  758,  998,  999, 1001, 1003, 1004,
 1007, 1013, 1017, 1019, 1020, 1024, 1025, 1026, 1027,  381,
 1035, 1043, 1044,  -90,  -79,  -79,    0,    0,    0,    0,
    0, 1000,  581,    0,    0,    0,  -79,  -17,  448,    0,
  371,    0,    0,    0,    0,    0,    0, 1681,    0,    0,
    0,    0,  109,  910,  -79, -179,    0,    0, -218,    0,
 5584,  362,    0,  805,  938,  938,  938,  938,    0,    0,
    0,  325, 1047,    0,  641,  320,    0,    0,  320,    0,
    0,    0,    0, 1048,    0,  -79,  320,  -79,  -79,    0,
  234,    0,  430,  910,  644,    0, 5442,    0,    0, 1051,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  581, 1054, 1055,
    0,  371, 1056,    0,  -79,    0,    0,  325, 1057, 1058,
 1059, 1070, 1077, 1060,    0,    0,  340, 1068, 2156,  648,
    0,    0,    0,    0,  464,  805,    0,    0,    0,  325,
    0,    0, 1082, 1083, 1078,    0,    0,    0,  758,  307,
    9,    0,    0,    0, 1061, 1087,    0,    0,  -79,  -79,
  -79,    0, 1068, 1081,    0, 1092,  861, 1089,  666, 1076,
  -79,  -79, 6162,    0,    0, 1090,  307,    0,    0,  910,
 1126, 1093, 1097, 1098,    0, 1381, 1099,    0,  805,    0,
 5654,    0,    0,  159,    0,    0, 1101,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1105,    0,    0,  325,  668,  172,  320,    0,
 1118,  805,    0, 1109,    0, 1126, 1108, 1116, 1106,    0,
 1122, 1125,  910,  805, 1127,  678,    0,    0,
  };
  protected static readonly short [] yyRindex = {            0,
    0, 1400, -185, 5834,    0,    0, 5218,   39, 4859, -175,
    0,    0,    0,  643,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, -185,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 5670,    0, 1075,    0,
    0,    0,    0, 3347, 3565, 5670,    0,    0,    0,    0,
    0, 2554,    0, 1136,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 4217, 4217,
 4217, 4217, 4217, 4217,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, -176,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 5670,    0,    0,    0,    0,
    0,    0,    0, 1151,    0,    0,    0, 1683,    0, 5670,
 5670,    0,    0,    0,    0,    0, 3008,    0,    0,    0,
    0,    0,  453,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 2819,    0,    0,
    0, 1137,    0, 3234, 3776,    0,    0, 3891,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1140,    0,
    0,    0,    0, 1141,    0,    0,    0,    0,    0,    0,
    0, -178,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 2480,    0, 2745, 1132,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 4432, 3460, 3663, 1947,    0,  885,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 1142,    0,    0,    0,    0,    0,    0,
    0,    0, 5670,    0,    0,    0, 5670,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1146,    0,    0,    0,    0,    0,    0,    0,
  507,    0, 1148,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  360,    0,    0,    0,
    0, 1417, 1132,    0,    0,  -93,    0,    0, 1157, 3121,
    0,  240, 4384,    0,    0,    0, 1947,    0,    0,    0,
    0, 3995,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 4217, 4217, 4217, 4217,    0,    0, 4217, 4217,    0,    0,
    0,    0,    0, 1144,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 6556,    0, 5670,
    0,    0,    0,    0,  682,    0,    0,    0,    0,    0,
    0,  684,    0,    0, 5670,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 6643,    0,    0,
    0,    0, 1045,    0,    0,    0,    0,  587,    0,    0,
    0,    0,    0,  780,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0, 1160,
    0, 1949, 5070,    0,  -26,    0,  410,    0,  419,    0,
    0,    0,    0,    0, 1358,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 1167,    0,    0,    0,    0,    0,    0,
    0,  253,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, -172,    0,
    0,    0,    0,    0,    0,    0,  313,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 4576, 1169,
 4102,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  345,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 5996,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  694,    0,    0,    0,  684,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0, 1138, 1267,  130,    0, 1161,
  507,  696,    0,    0,    0,    0,    0, 2215, 1890,    0,
    0, 2745,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  107,    0,
    0,    0,    0,    0,  959, 4480, 1947,    0,    0,    0,
    0,    0,    0,  240,    0,    0,    0,  703,    0,  709,
    0,    0,    0,    0,    0,    0, 1175,    0,    0,    0,
    0, 5670, 5670, 5670, 5670,    0,    0,    0,    0,    0,
 5670, 5670, 5670,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1185,    0,    0, 1358,    0, 6101,    0,    0,
    0, 6730,    0,    0,    0,    0,  721,    0,    0,    0,
 6817,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0, 1188,    0,    5,
    0, 1358,    0, 5730,  324,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
 4315,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0, 1890,    0,    0,    0,    0,    0,    0,
    0, 5670,    0, 1358, 2123, 2123, 2123, 2123,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  726,    0,  730, 5730,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  310,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0, 1185, 1358,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  324,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0, 1195,    0,    0, 1185,
    0,    0,  240,    0,    0,    0, 1191,    0,    0, 1890,
 1197,    0,    0,    0,    0,    0,    0,    0, 1358,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  740,    0,
    0, 1624,    0,    0,    0, 1201,    0, 1206,    0,    0,
    0,    0, 1890, 1358,    0,    0,    0,    0,
  };
  protected static readonly short [] yyGindex = {            0,
 1386,    0, 1261,    0, 1263, 1266, -193,    0,    0,    0,
    0,    0,    0,    0, -197, -164,   -6,   24, -198, -194,
    0,  -47,    0,  -12,    0,    0,    0,  800,    0,    0,
 -429,    0,    0, -268,    0,  -24,  717,    0, -150,  915,
  548, 1284,    0, -114,    0,    0,    0,  -89, -284,  -49,
 -896,    0, -873,    0, 1049, -445,  918,    0, -855,  -71,
    0,   66,    0,    0, -889,  -91,    0, -852, -214, -783,
  353, -145,    0,    0,    0,    0,    0,    0,    0,  -66,
    0, 1111,    0, -202,  331, -451,    0,  469,  406,    0,
  784, -512, -208,    0,    0,  597,    0,    0,    0,    0,
  976, -497,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  913, 1518,
    0,    0,  842,    0,  521,    0,  416,  857,    0,    0,
  681,    0,    0, 1521, 1454,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0, 1062,    0,
    0,    0,
  };
  protected static readonly short [] yyTable = {            55,
  215,   72,  249,   74,  318,   65,  277,  175,  452,  468,
  454,  178,  587,   93,  602,  602,  207,  737,  967,  218,
  361, 1003,  342,  340,  264,  671,  343,  339, 1013, 1035,
  223,  166,  750,  400,  325,  547,  216,   67,  138,   68,
   69,  209,   92,  395,  393,  410,  279,  396,  392, 1095,
  183, 1045,  216,  179,  542,  181,  341,  208,  287,  764,
  212,   67,  766,   68,   69, 1055,   67,  366,   68,   69,
   47,  324,  695,  209,  695,  695,  357,  394,  411,  420,
  426,  431,  607,   54,  607,  607,  578,  219,   67,  185,
   73,  992,  267,   54,  365,  993,  230,   54,  321,  230,
  230,  579,  994,  186,  455,  254,  276,  230,   67,  216,
   68,   69,  323,   54,  456,   50,  239,  448,  456,  139,
  288,  289,   52,  357,  670,  269,  270, 1100,  698,   52,
  239,  313,  433,  266,   67,   67,   68,   68,   69,   54,
 1089,   67,  271,   68,   69,  779,  139,  577,  444,  471,
  294,  216,  726,  727,   58,  312,  519,  209,  786,  265,
  140,  325,  325,  453,  226,  228,  230,  232,  234,  236,
  217,   67,  486,   68,   69,  103,  352,  392,  261,  316,
  550,  103, 1078,   54,  216,  103,  103,  580,  103,  216,
  325,  790,   54,  392,  397,  690,  319,  581,   56,  996,
  250,  251,  582,  562,  467,  481,  791,  445,  567, 1139,
  997,  699,  700,   67,  239,   68,   69,  447,  353,  287,
  354,  240,  355,  470,  280,  520,  252,  819,  607,  608,
  609,  610,  611,  612,  282,  283,  583,  613,  614,  615,
  616,  492,   67,   73,   73,  477,   77, 1158,   52,  357,
  239,  450, 1084,   48,  356,   60,  263,  733,  446,  945,
   70,   77, 1187, 1085,  473,  701,  702,  358,  359,  464,
  494, 1091, 1092, 1093,  462,  465, 1167,   78,  428,   62,
 1148,  466,  618,  619,  499,  526,  999, 1164,  532,  325,
  509,  510,   78,  480,  543, 1198,  326,  546,  326,   64,
 1017, 1018,  527, 1003,  528,  326,  995, 1206,  350,  600,
  600,  230,  296,  351,  358,  359,  182,  761,  326,  797,
  429,  516,   94, 1000,  522,  493,  216,  529,  496, 1205,
  592,    7,  537,  539,  469,  563, 1145,  870,  239,  430,
  871,  549,  688,  568,  400,  248,  760,  872,  321,  525,
   67,   95,   68,   69,  395,  393,  695,  954,  396,  392,
  482,  607,  515, 1166,  264,  521,  253,  592,   67,   96,
   68,   69,  287,  536,  538,  540,  157,  870,  218,  544,
 1088,  734,  548,  551,  591,  218,  110,  872,  394, 1025,
 1026, 1027, 1028,  517,  518,  111,  287,  668, 1029, 1030,
 1031,  706,  565,  886,  887,  287,  103,  103,  103,  696,
  451,  571,  574,  112,  697,  584,  768,  576,  769,  113,
  771,  772,  898,  773,  774, 1008,  595,  870,  162,   67,
 1189,   68,   69,  114,   54,  732,  366,  872,  280,  668,
  358,  359,   67,  705,   68,   69,  709,   54,  282,  283,
  668,  116,  609,  320,  455,  284,  285,  286,  668,  596,
  778,   48,  780,  585,  209,  184,  940,  689,  456, 1024,
  713,  287,  280,  413,  816,  281,  788,  731,  693,  694,
  736,  792,  282,  283,  210,  211,  209,  740,  691,  326,
  326,  326,  326,  326,  326,  749,  720,  721,  730,  280,
  287,  926,  280,  224,  735,  397,  738,  759,  178,  282,
  283,  178,  282,  283,  263,  907,  915,  744,  178,  908,
  916,  182,  876,  961,  182,  280,    7,  320,  739,  757,
   67,  182,   68,   69,  742,  593,  283,  414,  784,  237,
  752,  889,  415,  326,  326,  238,  486,  326,  758,  906,
  914,   67,  287,   68,  767,  782,   54,  722,  723,  776,
  777,  416,  417,   67,  295,   68,  716,  717,  718,  719,
  418,  268,  724,  725,  708, 1098,  788,   67,  219,   68,
  296,  219,   67,  297,   68,   69,  957,  925,  219,  296,
  793,  689,  973,  975,  424,  424,  424,   67,  877,   68,
   69,  315,  690,  598,  690,  690,  599,  296,  139,  692,
  690,  517,  325,  690,   67,  419,   68,   69,  873,   60,
  890,  225,  296,  690,  690,  280,  690,  296,  264,  165,
  296,  296,  165,  296,  296,  282,  283,  296,  464,  317,
  296,  296,  602,   60,  465,  227,  296,  296,  929,  280,
  896,  325, 1074,  296, 1033,  296,  296,  296,  322,  282,
  283,  296,  296,  296,  296,  296,   60,  296,  229,  284,
  285,  286,  296,  296,  296,  828,  829,  830,  831,  167,
  296,   67,  167,   68,   69,  888,  761,   67,  163,   68,
  928,  163,  278,  897,  689,   60,  934,  231,  280,  832,
  900,  899,  326,  284,  285,  286,  943,  362,  282,  283,
   60,  927,  233,  947,   60,  363,  235,  466,  239,   98,
  609,  442,  937,  744,  939,  987,  988,  989,  990,  672,
  284,  285,  286,  284,  285,  286,  933,  449,  689,  170,
  171,  172,  173,  174,  825,  826,  942, 1105,  944,  991,
  451,  801,  984,  802,  946,  460,  284,  285,  286,  930,
  931, 1057,  609,  948,   79,  960,   79,  609,  949,  461,
  609,  609,  950,  609,  609,  965,  272,  273,  274,  275,
  609,  609,  935,  936,  609,  472,  609,  609,  980,  290,
  291,  292,  293,  609,  966,  609,  609,  609,  474,  255,
  256,  257, 1001, 1126,  609,  609,  258,  259,  260,  475,
   67,  957,   68,  609,  609,   54, 1115,  952,  953,  366,
  609,  554,  555,  556,  557, 1140,  968,  969,  970,  971,
  972,  974,  976,  977,  978,  979,  476,  981,  982,  216,
  673,  674,  675,  676,  690,  690,  690,  963,  964, 1044,
  489,  957,  497, 1086,  637,  494,  284,  285,  286,  886,
  887, 1017, 1018,  998,   67,   97,   68,   69,  498,  677,
  678,  679,  680, 1009, 1010,  139, 1034,  500,   98,  501,
  284,  285,  286, 1038,  691,  681,  502,  682,  683, 1042,
   99,  100,  101,  102,  103,  104,  503,   67,  504,   68,
   69, 1192,  505,  784, 1011, 1012,  506,  637, 1043,  936,
  602,  744, 1107,  964,  466, 1118,  964, 1058,  637, 1137,
  964, 1082,  507,  637, 1037,  508,  466,  511, 1051,  284,
  285,  286, 1053, 1101, 1102, 1103, 1104, 1160,  964, 1193,
  964,  535,  637,  637,  105,  106, 1133,  957,  107, 1208,
  964,  637,  602,  505,  505,  451,  451,  602,  693,  541,
  602,  602,  552,  602,  602,  454,  454,   81,   81,  561,
  602,  602, 1099,  878,  580,  580,  602,  602, 1079, 1080,
  582,  582,  263,  602,  564,  602,  602,  602, 1108,  566,
 1083, 1109,  456,  456,  602,  602,  637,   82,   82, 1112,
  957,  414,  414,  602,  602, 1116, 1121,  572, 1090, 1094,
  602,  417,  417,  569,  570,   67,  684,   68,  573,  689,
  588,  575,  597,  604,  605,  666,  280,  685,  686,  687,
  422,  669, 1119,  668,  695,  707,  282,  283,  142, 1111,
  728, 1113, 1114,  748,  751,  753, 1157,  656,  755,   99,
  100,  101,  102,  103,  104,  143,  144,  145,  146,  147,
  148,  149,  150,  754,  151,  770,  152,  153,  154,  155,
  775, 1058,  423,  264, 1136,  689,  787,  424, 1125,  789,
  818,  795,  796,    7,  673,  674,  675,  676,  798,  656,
  803,  425,  804,  805,  656,  806,  807,  689,  808,  809,
  656,  810,  811,  105,  106,  812,  813,  107,  656,  158,
  814,  159,  160,  677,  678,  679,  680,  815,  434,  435,
  436,  817, 1152, 1153, 1154,  437,  438,  439,  821,  681,
  822,  682,  683,  823, 1162, 1163,  595,  824,  874,  827,
  881, 1195,  691,  879,  691,  691,  880,  882,  883,  261,
  103,  691,  691,  691,  691,  691,  691,  691,  163,  691,
  884,  894,  691,  691,  691,  918,  691,  691,  691,  919,
  920,  691,  691,  689,  691,  921,  691,  691,  691,  691,
  691, 1194,  691,  691,  691,  691,  924,  691,  691,  932,
  691,  691,  941,  955,  691,  962,  983,  405,  691,  691,
  985,  802,  691,  691,  691,  691,  691,  691,  691,  691,
  986,  691,  691,  691, 1005,  313,  691, 1014,  691,  691,
 1015,  691,  691, 1016,  691,  691,  693,  691,  691,  691,
 1023, 1022, 1032,  691,  691,  691,  691,  691, 1036,  691,
  691, 1040, 1039, 1046,  691,  691,  691, 1048,  691,  691,
    7,  691,  691,  691,  691,  691, 1047,  284,  285,  286,
  684,  406, 1049, 1052, 1056, 1054,  407, 1060,  693, 1081,
 1061,  691, 1062,  693, 1063, 1064,  693,  693, 1065,  693,
  693,   17,   18,  693, 1066,  408,  693,  693, 1067,  691,
 1068, 1069,  693,  693,  409, 1070, 1071, 1072, 1073,  693,
  910,  693,  693,  693,  691,  691, 1075,  693,  693,  693,
  693,  693,  619,  693, 1076, 1077,  691, 1106,  693,  693,
  693, 1120, 1110, 1122, 1123, 1124,  693, 1134, 1127, 1128,
 1149, 1132, 1129,  691,  691,  691,  691,  691,  691,  691,
  691,  325,  691, 1130,  691,  691,  691,  691, 1143,   67,
 1131,   68,   69,    7,  325, 1141, 1142, 1150, 1156, 1159,
  463, 1165, 1161,  911, 1180,  619,   11,   12, 1181, 1182,
  282,  283, 1190, 1186,  140, 1191,  619, 1197, 1199, 1200,
  912,  619,  691,  691,  691,  691,  691,  691,  964,  691,
  691,  913, 1203, 1202,  691, 1204,  619,  619, 1207,    1,
  619,  691,   39,  661,  163,  659,  612,  643,  103,  619,
  103,  103,  630,  682,   69,  683,  104,  103,  103,  103,
  103,  103,  103,  103,  550,  103,   70,  631,  103,  103,
  103,  691,  103,  103,  554,  691,  691,  103,  103,   73,
  103,  516,  103,  103,  103,  103,  103,  659,  103,  103,
  103,  103,  659,  103,  103,   75,  103,  103,  659,  411,
  103,  296,  320,  350,  103,  103,  659,  351,  103,  103,
  103,  103,  103,  103,  103,  103,  122,  103,  103,  103,
  220,  336,  103,  337,  103,  103,  338,  103,  103,  885,
  103,  103,  781,  103,  103,  103,  141,  951, 1050,  103,
  103,  103,  103,  103,  344,  103,  103, 1168, 1184,  603,
  103,  103,  103,  553,  103,  103,  794,  103,  103,  103,
 1196,  103, 1117,  142, 1144,  325,  325,  325,  325,  325,
  325,  938, 1041,  765,  657,   91,  820,  103,  895, 1096,
  143,  144,  145,  146,  147,  148,  149,  150, 1155,  151,
  893,  152,  153,  154,  155,  103, 1007, 1169, 1170, 1171,
 1172, 1173, 1174, 1175, 1176, 1177, 1178,  115,  247,    0,
    0,  103,    0,    0,    0,  729,  657,    0,    0,  325,
  325,  657,  103,  325,    0,    0,    0,  657,    0,  156,
  157,  284,  285,  286,  158,  657,  159,  160,    0,  103,
  103,  103,  103,  103,  103,  103,  103,    0,  103,    0,
  103,  103,  103,  103,    0,  393,    0,  393,  393,    0,
    0,    0,    0,    0,    0,    0,  393,    0,    0,  419,
  419,    0,    0,    0,    0,    0,    0,    0,  161,    0,
  393,    0,  162,  163,    0,    0,    0,    0,  103,  103,
  103,  103,  103,  103,    0,  103,  103,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  103, 1179,    0,
    0,    0,    0,    0,  104,    0,  104,  104,    0,    0,
    0,    0,  107,  104,  104,  104,  104,  104,  104,  104,
    0,  104,    0,    0,  104,  104,  104,  103,  104,  104,
    0,  103,  103,  104,  104,    0,  104,    0,  104,  104,
  104,  104,  104,    0,  104,  104,  104,  104,    0,  104,
  104,    0,  104,  104,    0,    0,  104,    0,    0,    0,
  104,  104,    0,    0,  104,  104,  104,  104,  104,  104,
  104,  104,    0,  104,  104,  104,    0,    0,  104,    0,
  104,  104,    0,  104,  104,    0,  104,  104,    0,  104,
  104,  104,  393,    0,    0,  104,  104,  104,  104,  104,
    0,  104,  104,    0,    0,    0,  104,  104,  104,    0,
  104,  104,    0,  104,  104,  104,    0,  104,    0,  393,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  104,    0,    0,  393,  393,  393,  393,
  393,  393,  393,  393,    0,  393,    0,  393,  393,  393,
  393,  104,    0,    0,    0,    0,    0,    0,    0,    0,
  819,  607,  608,  609,  610,  611,  612,  104,    0,    0,
  613,  614,  615,  616,    0,    0,    0,    0,  104,    0,
    0,    0,    0,    0,    0,  393,  393,    0,    0,    0,
  393,    0,  393,  393,    0,  104,  104,  104,  104,  104,
  104,  104,  104,    0,  104,    0,  104,  104,  104,  104,
    0,  393,    0,  393,  393,  618,  619,    0,    0,  999,
    0,    0,  393,    0,  419,    0,  419,    0,    0,    0,
    0,    0,    0,    0,  393,    0,  393,    0,  393,  393,
    0,    0,    0,    0,  104,  104,  104,  104,  104,  104,
    0,  104,  104,    0,    0,    0,    0,    0,    0,    0,
    0,    0, 1183,  104,    0,    0,    0,    0,    0,    0,
  107,    0,  107,  107,    0,    0,    0,    0,  105,  107,
  107,  107, 1087,  107,  107,  107,    0,  107,    0,    0,
  107,  107,  107,  104,    0,  107,    0,  104,  104,  107,
  107,    0,  107,    0,  107,  107,  107,  107,  107,    0,
  107,  107,  107,  107,    0,  107,  107,    0,  107,  107,
    0,    0,  107,    0,    0,    0,  107,  107,    0,    0,
  107,  107,  107,  107,  107,  107,  107,  107,    0,  107,
  107,  107,    0,    0,  107,    0,  107,  107,    0,  107,
  107,    0,  107,  107,    0,  107,  107,  107,  393,    0,
    0,  107,  107,  107,  107,  107,    0,  107,  107,    0,
    0,    0,  107,  107,  107,    0,  107,  107,    0,  107,
  107,  107,    0,    0,    0,  393,    0,    0,    0,    0,
    0,    0,    0,  673,  674,  675,  676,    0,    0,  107,
    0,    0,  393,  393,  393,  393,  393,  393,  393,  393,
    0,  393,    0,  393,  393,  393,  393,  107,    0,    0,
    0,    0,  677,  678,  679,  680,    0,    0,    0,    0,
    0,    0,    0,  107,    0,    0,    0,    0,  681,    0,
  682,  683,    0,    0,  107,    0,    0,    0,    0,    0,
    0,  393,  393,    0,    0,    0,  393,    0,  393,  393,
    0,  107,  107,  107,  107,  107,  107,  107,  107,    0,
  107,    0,  107,  107,  107,  107,    0,  393,    0,  393,
  393,    0,    0,    0,    0,    0,    0,    0,  393,    0,
    0,  410,    0,    0,    0,    0,    0,    0,    0,    0,
  393,    0,  393,    0,  393,  393,    0,    0,    0,    0,
  107,  107,  107,  107,  107,  107,    0,  107,  107,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  107,
    0,    0,    0,    0,    0,    0,  105,    0,  105,  105,
    0,    0,    0,    0,  106,  105,  105,  105,  375,  105,
  105,  105,    0,  105,    0,    0,  105,  105,  105,  107,
    0,  105,    0,  107,  107,  105,  105,    0,  105,  684,
  105,  105,  105,  105,  105,    0,  105,  105,  105,  105,
    0,  105,  105,    0,  105,  105,    0,    0,  105,    0,
    0,    0,  105,  105,    0,    0,  105,  105,  105,  105,
  105,  105,  105,  105,    0,  105,  105,  105,    0,    0,
  105,    0,  105,  105,    0,  105,  105,    0,  105,  105,
    0,  105,  105,  105,  393,    0,    0,  105,  105,  105,
  105,  105,    0,  105,  105,    0,    0,    0,  105,  105,
  105,    0,  105,  105,    0,  105,  105,  105,    0,    0,
    0,  393,    0,    0,    0,    0,    0,    0,    0,  375,
  375,  375,  375,    0,    0,  105,    0,    0,  393,  393,
  393,  393,  393,  393,  393,  393,    0,  393,    0,  393,
  393,  393,  393,  105,    0,    0,    0,    0,  375,  375,
  375,  375,    0,    0,    0,  901,    0,    0,    0,  105,
    0,    0,    0,    0,  375,    0,  375,  375,    0,    0,
  105,    0,    0,    0,    0,    0,    0,  393,  393,    0,
  693,    0,  393,    0,  393,  393,    0,  105,  105,  105,
  105,  105,  105,  105,  105,  902,  105,    0,  105,  105,
  105,  105,    0,   67,    0,   68,   69,    0,    7,    0,
    0,    0,    0,    0,  139,    0,    0,  903,    0,    0,
  693,   11,   12,    0,    0,    0,  393,    0,  140,    0,
  393,  393,    0,  693,    0,  904,  105,  105,  105,  105,
  105,  105,  693,  105,  105,  905,  693,  693,    0,    0,
    0,    0,    0,    0,    0,  105,    0,    0,    0,    0,
  693,    0,  106,    0,  106,  106,    0,    0,    0,   75,
  693,  106,  106,  106,    0,  106,  106,  106,    0,  106,
    0,    0,  106,  106,  106,  105,    0,  106,    0,  105,
  105,  106,  106,    0,  106,  375,  106,  106,  106,  106,
  106,    0,  106,  106,  106,  106,    0,  106,  106,    0,
  106,  106,    0,    0,  106,    0,    0,    0,  106,  106,
    0,    0,  106,  106,  106,  106,  106,  106,  106,  106,
    0,  106,  106,  106,    0,    0,  106,    0,  106,  106,
    0,  106,  106,  603,  106,  106,    0,  106,  106,  106,
  141,    0,    0,  106,  106,  106,  106,  106,    0,  106,
  106,    0,    0,    0,  106,  106,  106,    0,  106,  106,
    0,  106,  106,  106,    0,    0,    0,  142,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  106,    0,    0,  143,  144,  145,  146,  147,  148,
  149,  150,    0,  151,    0,  152,  153,  154,  155,  106,
    0,    0,    0,    0,    0,    0,  606,  607,  608,  609,
  610,  611,  612,    0,    0,  106,  613,  614,  615,  616,
    0,    0,    0,    0,    0,    0,  106,    0,    0,    0,
    0,    0,    0,  156,  157,    0,    0,    0,  158,    0,
  159,  160,    0,  106,  106,  106,  106,  106,  106,  106,
  106,    0,  106,    0,  106,  106,  106,  106,    0,    0,
  617,  618,  619,    0,    0,  620,    0,  819,  607,  608,
  609,  610,  611,  612,    0,    0,    0,  613,  614,  615,
  616,    0,  161,    0,    0,    0,  162,  163,    0,    0,
    0,    0,  106,  106,  106,  106,  106,  106,    0,  106,
  106,    0,    0,    0,    0,    0,    0,    0,  621,    0,
    0,  106,    0,    0,    0,    0,    0,   75,    0,   75,
   75,  617,  618,  619,   75,    0,   75,   75,   75,    0,
   75,   75,   75,    0,   75,    0,    0,    0,   75,   75,
    0,  106,   75,    0,    0,  106,  106,   75,    0,   75,
    0,   75,   75,   75,   75,   75,    0,   75,   75,   75,
   75,    0,   75,   75,    0,   75,   75,    0,    0,   75,
    0,    0,    0,   75,   75,    0,    0,   75,   75,   75,
   75,   75,   75,   75,   75,    0,   75,   75,   75,    0,
    0,   75,    0,   75,   75,    0,   75,   75,  604,   75,
   75,  603,   75,   75,   75,    0,    0,    0,   75,   75,
   75,   75,   75,    0,   75,   75,    0,    0,    0,   75,
   75,   75,    0,   75,   75,    0,   75,   75,   75,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  603,    0,    0,   75,    0,  603,    0,
    0,  603,  603,    0,  603,  603,    0,    0,    0,    0,
    0,  603,  603,    0,   75,    0,    0,  603,  603,    0,
    0,    0,    0,    0,  603,    0,  603,  603,  603,    0,
   75,    0,    0,    0,    0,  603,  603,    0,    0,    0,
    0,   75,    0,    0,  603,  603,    0,    0,    0,    0,
    0,  603,    0,    0,    0,    0,    0,    0,   75,   75,
   75,   75,   75,   75,   75,   75,    0,   75,    0,   75,
   75,   75,   75,    0,  170,  171,  172,  173,  174,    0,
    0,    0,    0,    0,    0,    0,    0,  187,  188,  189,
  190,    0,  191,  192,  193,  194,  195,  196,  197,    0,
    0,    0,    0,    0,    0,  198,    0,   75,   75,   75,
   75,   75,   75,    0,   75,   75,    0,    0,  199,  200,
  201,  202,  203,  204,    0,    0,   75,    0,    0,    0,
    0,    0,   75,    0,   75,   75,    0,  544,    0,    0,
    0,   75,   75,   75,    0,   75,   75,   75,    0,   75,
    0,    0,    0,   75,   75,    0,   75,   75,    0,    0,
   75,   75,   75,    0,   75,    0,   75,   75,   75,   75,
   75,    0,   75,   75,   75,   75,    0,   75,   75,    0,
   75,   75,    0,    0,   75,    0,    0,    0,   75,   75,
    0,    0,   75,   75,   75,   75,   75,   75,   75,   75,
    0,   75,   75,   75,    0,    0,   75,    0,   75,   75,
    0,   75,   75,    0,   75,   75,  604,   75,   75,   75,
    0,    0,    0,   75,   75,   75,   75,   75,    0,   75,
   75,    0,    0,    0,   75,   75,   75,    0,   75,   75,
    0,   75,   75,   75,  205,  206,    0,    0,    0,    0,
  545,    0,    0,    0,    0,    0,    0,    0,  604,    0,
    0,   75,    0,  604,    0,    0,  604,  604,    0,  604,
  604,    0,    0,    0,    0,    0,  604,  604,    0,   75,
    0,    0,  604,  604,    0,    0,    0,    0,    0,  604,
    0,  604,  604,  604,    0,    0,    0,    0,    0,    0,
  604,  604,    0,    0,    0,    0,   75,    0,    0,  604,
  604,    0,    0,    0,    0,    0,  604,    0,    0,    0,
    0,    0,    0,   75,   75,   75,   75,   75,   75,   75,
   75,    0,   75,    0,   75,   75,   75,   75,   75,   76,
   77,   78,   79,   80,   81,   82,   83,   84,   85,   86,
   87,   88,   89,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  461,    0,    0,    0,    0,    0,    0,
    0,    0,   75,   75,   75,   75,   75,   75,    0,   75,
   75,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   75,    0,    0,    0,  544,    0,  544,    0,    0,
    0,    0,    0,    0,  544,  544,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,   75,    0,    0,    0,   75,   75,  544,    0,  544,
  544,  544,  544,  544,    0,  544,  544,  544,  544,    0,
  544,  544,    0,  544,  544,  544,    0,  544,    0,    0,
    0,    0,  544,    0,    0,  544,  544,    0,  544,  544,
  544,  544,  544,    0,  544,  544,  544,  544,  544,  544,
    0,  544,  544,    0,  544,  544,   29,  544,  544,    0,
  544,  544,  544,    0,    0,  544,  544,  544,  544,  544,
  544,    0,  544,  544,  544,  544,  544,  544,  544,  544,
    0,  544,  544,    0,  544,  544,  544,    0,  545,    0,
  545,    0,    0,    0,    0,    0,    0,  545,  545,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  545,    0,  545,  545,  545,  545,  545,  544,  545,  545,
  545,  545,    0,  545,  545,    0,  545,  545,  545,    0,
  545,    0,    0,    0,    0,  545,    0,    0,  545,  545,
    0,  545,  545,  545,  545,  545,    0,  545,  545,  545,
  545,  545,  545,    0,  545,  545,    0,  545,  545,   30,
  545,  545,    0,  545,  545,  545,    0,    0,  545,  545,
  545,  545,  545,  545,    0,  545,  545,  545,  545,  545,
  545,  545,  545,    0,  545,  545,    0,  545,  545,  545,
    0,  461,    0,  461,  461,    0,    0,    0,    0,    0,
  461,  461,    0,    0,  461,    0,    0,    0,  461,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  461,    0,  461,  461,  461,  461,  461,
  545,  461,  461,  461,  461,    0,  461,  461,    0,  461,
  461,    0,    0,  461,    0,    0,    0,    0,  461,    0,
    0,  461,  461,    0,  461,  461,  461,  461,  461,    0,
  461,  461,  461,    0,   26,  461,    0,  461,  461,    0,
  461,  461,    0,  461,  461,    0,  461,  461,  461,    0,
    0,    0,  461,  461,  461,  461,  461,    0,  461,  461,
    0,    0,    0,  461,  461,  461,    0,  461,  461,    0,
  461,  461,  461,    0,   29,    0,   29,    0,    0,    0,
    0,    0,    0,   29,   29,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,   29,    0,   29,   29,
   29,   29,   29,    0,   29,   29,   29,   29,    0,   29,
   29,    0,   29,   29,   29,    0,   29,    0,    0,    0,
    0,   29,   28,    0,   29,   29,    0,   29,   29,   29,
   29,   29,    0,   29,   29,   29,   29,   29,    0,    0,
   29,   29,    0,    0,   29,    0,   29,   29,    0,   29,
   29,   29,    0,    0,   29,   29,   29,   29,   29,   29,
    0,   29,    0,    0,   29,   29,   29,   29,   29,    0,
   29,    0,    0,   29,   29,   29,    0,   30,    0,   30,
    0,    0,    0,    0,    0,    0,   30,   30,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,   30,
    0,   30,   30,   30,   30,   30,    0,   30,   30,   30,
   30,    0,   30,   30,    0,   30,   30,   30,    0,   30,
    0,    0,    0,    0,   30,  562,    0,   30,   30,    0,
   30,   30,   30,   30,   30,    0,   30,   30,   30,   30,
   30,    0,    0,   30,   30,    0,    0,   30,    0,   30,
   30,    0,   30,   30,   30,    0,    0,   30,   30,   30,
   30,   30,   30,    0,   30,    0,    0,   30,   30,   30,
   30,   30,   26,   30,    0,    0,   30,   30,   30,    0,
    0,   26,   26,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   26,    0,   26,   26,   26,   26,
   26,    0,   26,   26,   26,   26,    0,   26,   26,    0,
   26,   26,   26,    0,   26,    0,    0,    0,    0,   26,
    0,    0,   26,   26,    0,   26,   26,   26,   26,   26,
  564,   26,   26,   26,   26,   26,    0,    0,   26,   26,
    0,    0,   26,    0,   26,   26,    0,   26,   26,   26,
    0,    0,   26,   26,   26,   26,   26,   26,    0,   26,
   28,    0,   26,   26,   26,   26,   26,    0,   26,   28,
   28,   26,   26,   26,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,   28,    0,   28,   28,   28,   28,   28,    0,
   28,   28,   28,   28,    0,   28,   28,    0,   28,   28,
   28,    0,   28,    0,    0,    0,    0,   28,    0,    0,
   28,   28,    0,   28,   28,   28,   28,   28,    0,   28,
   28,   28,   28,   28,  459,    0,   28,   28,    0,    0,
   28,    0,   28,   28,    0,   28,   28,   28,    0,    0,
   28,   28,   28,   28,   28,   28,    0,   28,    0,    0,
   28,   28,   28,   28,   28,    0,   28,    0,    0,   28,
   28,   28,    0,  562,    0,  562,    0,    0,    0,    0,
    0,    0,  562,  562,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  562,    0,  562,  562,  562,
  562,  562,    0,  562,  562,  562,  562,    0,  562,  562,
    0,  562,  562,    0,    0,  562,    0,    0,    0,    0,
  562,    0,    0,  562,  562,    0,  562,  562,  562,  562,
  562,  460,  562,  562,  562,    0,    0,  562,    0,  562,
  562,    0,  562,  562,    0,  562,  562,    0,  562,  562,
  562,    0,    0,    0,  562,  562,  562,  562,  562,    0,
  562,  562,    0,    0,    0,  562,  562,  562,    0,  562,
  562,    0,  562,  562,  562,    0,    0,    0,  564,    0,
  564,    0,    0,    0,    0,    0,    0,  564,  564,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  564,    0,  564,  564,  564,  564,  564,    0,  564,  564,
  564,  564,    0,  564,  564,    0,  564,  564,    0,    0,
  564,    0,    0,    0,    0,  564,    0,    0,  564,  564,
    0,  564,  564,  564,  564,  564,  277,  564,  564,  564,
    0,    0,  564,    0,  564,  564,    0,  564,  564,    0,
  564,  564,    0,  564,  564,  564,    0,    0,    0,  564,
  564,  564,  564,  564,    0,  564,  564,    0,    0,    0,
  564,  564,  564,    0,  564,  564,    0,  564,  564,  564,
    0,  459,  459,    0,    0,  459,    0,    0,    0,  459,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  459,    0,  459,  459,  459,  459,
  459,    0,  459,  459,  459,  459,    0,  459,  459,    0,
  459,  459,    0,    0,  459,    0,    0,    0,    0,  459,
    0,    0,  459,  459,  316,  459,  459,  459,  459,  459,
    0,  459,  459,  459,    0,    0,  459,    0,  459,  459,
    0,  459,  459,    0,  459,  459,    0,  459,  459,  459,
    0,    0,    0,  459,  459,  459,  459,  459,    0,  459,
  459,    0,    0,    0,  459,  459,  459,    0,  459,  459,
    0,  459,  459,  459,    0,    0,    0,    0,  460,  460,
    0,    0,  460,    0,    0,    0,  460,    0,    0,    0,
    0,    0,    0,  294,    0,    0,    0,    0,    0,    0,
    0,  460,    0,  460,  460,  460,  460,  460,    0,  460,
  460,  460,  460,    0,  460,  460,    0,  460,  460,    0,
    0,  460,    0,    0,    0,    0,  460,    0,    0,  460,
  460,    0,  460,  460,  460,  460,  460,    0,  460,  460,
  460,  606,    0,  460,    0,  460,  460,    0,  460,  460,
    0,  460,  460,    0,  460,  460,  460,    0,    0,    0,
  460,  460,  460,  460,  460,    0,  460,  460,    0,    0,
    0,  460,  460,  460,    0,  460,  460,    0,  460,  460,
  460,    0,    0,    0,  277,    0,  277,    0,    0,  609,
    0,    0,    0,  277,  277,    0,    0,    0,    0,  277,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  277,    0,  277,  277,
  277,  277,  277,    0,  277,  277,  277,  277,    0,  277,
  277,    0,  277,  277,    0,    0,  277,    0,    0,    0,
    0,  277,    0,    0,  277,  277,    0,  277,  277,  277,
  277,  277,    0,  277,  277,  277,    0,    0,    0,    0,
  277,  277,    0,    0,  277,    0,  277,  277,    0,  277,
  277,  277,    0,    0,    0,  277,  277,  277,  277,  277,
    0,  277,  316,    0,  316,    0,  277,  277,  277,    0,
  277,  316,  316,  277,  277,  277,    0,    0,    0,    0,
    0,  316,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  316,    0,  316,  316,  316,  316,
  316,    0,  316,  316,  316,  316,    0,  316,  316,    0,
  316,  316,    0,    0,  316,    0,    0,    0,    0,  316,
    0,    0,  316,  316,    0,  316,  316,  316,  316,  316,
    0,  316,  316,  316,    0,    0,    0,    0,  316,  316,
    0,  294,  316,    0,  316,  316,    0,  316,  316,  316,
  294,  294,    0,  316,  316,  316,  316,  316,    0,  316,
    0,    0,    0,    0,  316,  316,  316,    0,  316,    0,
    0,  316,  316,  316,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  294,    0,    0,    0,    0,  294,  606,
    0,  294,  294,    0,  294,  294,    0,    0,  294,    0,
    0,  294,  294,    0,    0,    0,    0,  294,  294,    0,
    0,    0,    0,    0,  294,    0,  294,  294,  294,    0,
    0,    0,  294,  294,  294,  294,  294,    0,  294,    0,
    0,  606,    0,  294,  294,  294,  606,  609,    0,  606,
  606,  294,  606,  606,    0,    0,    0,    0,    0,  606,
  606,    0,    0,    0,    0,  606,  606,    0,    0,    0,
    0,    0,  606,    0,  606,  606,  606,    0,    0,    0,
    0,    0,    0,  606,  606,    0,    0,    0,    0,  609,
    0,    0,  606,  606,  609,    0,    0,  609,  609,  606,
  609,  609,    0,    0,    0,    0,    0,  609,  609,    0,
    0,    0,    0,  609,  609,    0,    0,    0,    0,    0,
  609,    0,  609,  609,  609,    0,    0,    0,    0,    0,
    0,  609,  609,  392,    0,  392,  392,    0,    0,    0,
  609,  609,  392,  392,  392,    0,  392,  609,  392,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  392,  392,
    0,    0,  392,    0,    0,  392,    0,  392,  392,  392,
  392,  392,    0,  392,  392,  392,  392,    0,  392,  392,
    0,  392,  392,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  392,  392,  392,  392,
    0,    0,  392,    0,    0,    0,    0,    0,    0,  392,
  392,    0,    0,  392,    0,  392,    0,    0,    0,    0,
    0,    0,    0,    0,  392,    0,  392,  392,  392,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  392,
    0,    0,  392,    0,  392,    0,    0,    0,    0,    0,
    0,    0,    0,    0,   67,    0,   68,   69,    0,    0,
    0,    0,    0,    0,    0,  139,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  140,
  392,    0,    0,    0,    0,    0,    0,   67,    0,   68,
   69,    0,    0,    0,    0,    0,    0,    0,  463,    0,
    0,    0,    0,    0,    0,    0,    0,  392,  282,  283,
    0,    0,  140,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  392,  392,  392,  392,  392,  392,
  392,  392,    0,  392,    0,  392,  392,  392,  392,    0,
    0,    0,    0,  464,    0,    0,    0,    0,    0,  465,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  392,  392,    0,    0,    0,  392,    0,
  392,  392,    0,  298,    0,  392,    0,  299,  300,  301,
  302,  303,  304,    0,  305,  306,  307,  308,  309,  310,
  311,  141,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  277,    0,  277,  277,
    0,    0,  392,    0,    0,    0,  392,  392,  142,    0,
    0,    0,    0,    0,  141,    0,    0,    0,    0,    0,
    0,  277,    0,    0,    0,  143,  144,  145,  146,  147,
  148,  149,  150,    0,  151,    0,  152,  153,  154,  155,
    0,  142,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  143,  144,
  145,  146,  147,  148,  149,  150,    0,  151,    0,  152,
  153,  154,  155,    0,  156,  157,    0,    0,    0,  158,
    0,  159,  160,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  156,  157,  284,
  285,  286,  158,    0,  159,  160,    0,    0,    0,    0,
    0,    0,    0,  161,    0,  277,    0,  162,  163,  277,
  277,  277,  277,  277,  277,    0,  277,  277,  277,  277,
  277,  277,  277,  277,    0,    0,   67,    0,   68,   69,
    0,    0,    0,    0,    0,    0,  161,  463,    0,    0,
  162,  163,    0,    0,    0,    0,    0,  593,  283,    0,
  277,  140,    0,    0,   67,    0,   68,   69,    0,    0,
    0,    0,    0,    0,    0,    0,    0,  277,  277,  277,
  277,  277,  277,  277,  277,    0,  277,    0,  277,  277,
  277,  277,  464,    0,    0,    0,    0,  132,  465,  132,
  132,    0,    0,    0,    0,    0,    0,    0,  132,    0,
    0,    0,    0,   67,    0,   68,   69,    0,  132,  132,
    0,    0,  132,    0,  213,    0,  277,  277,    0,    0,
    0,  277,    0,  277,  277,    0,    0,    0,  140,    3,
    0,    0,    0,    0,    4,    0,    0,    5,    6,    0,
    7,    8,    0,  132,    0,    0,    0,    9,   10,  132,
    0,    0,    0,   11,   12,    0,    0,    0,    0,    0,
   13,    0,   14,   15,   16,  277,    0,    0,    0,  277,
  277,   17,   18,  141,    0,  119,    0,    0,    0,    0,
   19,   20,    0,    0,    0,    0,    0,   21,  120,  121,
    0,    0,  122,    0,  123,    0,    0,    0,    0,    0,
  142,  124,    0,  125,  126,  127,  128,  129,  130,  131,
  132,    0,  133,  134,  135,    0,    0,  143,  144,  145,
  146,  147,  148,  149,  150,    0,  151,    0,  152,  153,
  154,  155,    0,    0,  132,  548,    0,  548,  548,    0,
    0,  703,    0,  704,    0,    0,  548,    0,    0,    0,
  141,   67,    0,   68,   69,    0,    0,    0,    0,    0,
  548,  132,  139,    0,    0,    0,  156,  157,  284,  285,
  286,  158,    0,  159,  160,    0,  140,  142,  132,  132,
  132,  132,  132,  132,  132,  132,    0,  132,    0,  132,
  132,  132,  132,    0,  143,  144,  145,  146,  147,  148,
  149,  150,    0,  151,    0,  152,  153,  154,  155,    0,
    0,   67,    0,   68,   69,  161,    0,    0,    0,  162,
  163,    0,  213,    0,    0,    0,    0,  132,  132,  132,
  132,  132,  132,    0,  132,  132,  140,    0,    0,    0,
  136,    0,    0,  156,  157,    0,  137,    0,  158,    0,
  159,  160,    0,    0,    0,  548,  548,  548,  548,  548,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  710,    0,    0,    0,    0,  132,    0,    0,    0,
  132,  132,  548,   67,    0,   68,   69,    0,    0,  711,
    0,  712,  161,    0,  213,    0,  162,  163,  141,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  140,  548,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  142,  548,  548,  548,  548,
  548,  548,  548,  548,    0,  548,    0,  548,  548,  548,
  548,    0,  143,  144,  145,  146,  147,  148,  149,  150,
    0,  151,    0,  152,  153,  154,  155,    0,  141,   67,
    0,   68,   69,    0,    0,    0,    0,    0,    0,    0,
  479,    0,    0,    0,    0,  548,  548,    0,    0,    0,
  548,    0,  548,  548,  140,  142,    0,    0,    0,    0,
    0,  156,  157,    0,    0,    0,  158,    0,  159,  160,
    0,    0,  143,  144,  145,  146,  147,  148,  149,  150,
    0,  151,    0,  152,  153,  154,  155,    0,    0,    0,
    0,    0,    0,   67,  548,   68,   69,    0,  548,  548,
  141,    0,    0,    0,  213,    0,    0,    0,    0,   67,
  161,   68,   69,    0,  162,  163,    0,    0,  140,    0,
  743,  530,  157,    0,  328,    0,  158,  142,  159,  160,
    0,    0,    0,    0,  140,    0,    0,    0,    0,    0,
    0,    0,  531,    0,  143,  144,  145,  146,  147,  148,
  149,  150,    0,  151,    0,  152,  153,  154,  155,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  161,  329,    0,    0,  162,  163,  141,    7,    8,    0,
    0,  330,    0,    0,    9,    0,    0,    0,    0,    0,
   11,   12,    0,  156,  157,    0,    0,   13,  158,    0,
  159,  160,    0,  142,    0,  331,  332,  333,   17,   18,
    0,  334,    0,    0,    0,    0,  335,    0,    0,    0,
  143,  144,  145,  146,  147,  148,  149,  150,    0,  151,
    0,  152,  153,  154,  155,    0,    0,    0,    0,    0,
  141,   67,  161,   68,   69,    0,  162,  163,    0,    0,
    0,    0, 1188,    0,    0,    0,  141,  171,    0,  171,
  171,    0,    0,    0,    0,    0,  140,  142,  171,  156,
  157,    0,    0,    0,  158,    0,  159,  160,    0,    0,
    0,    0,  171,  142,  143,  144,  145,  146,  147,  148,
  149,  150,    0,  151,    0,  152,  153,  154,  155,    0,
  143,  144,  145,  146,  147,  148,  149,  150,    0,  151,
    0,  152,  153,  154,  155,    0,    0,  393,  161,  393,
  393,    0,  162,  163,    0,    0,    0,    0,  393,    0,
    0,    0,    0,  545,  157,    0,    0,    0,  158,    0,
  159,  160,  393,    0,    0,  142,    0,    0,    0,  156,
  157,    0,    0,    0,  158,    0,  159,  160,    0,    0,
    0,    0,  143,  144,  145,  146,  147,  148,  149,  150,
    0,  151,    0,  152,  153,  154,  155,    0,    0,    0,
    0,    0,  161,    0,    0,    0,  162,  163,  141,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  161,    0,
    0,    0,  162,  163,  171,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,  142,  158,    0,  159,  160,
    0,   42,    0,   42,   42,    0,    0,    0,    0,    0,
    0,  171,  143,  144,  145,  146,  147,  148,  149,  150,
    0,  151,    0,  152,  153,  154,  155,    0,  171,  171,
  171,  171,  171,  171,  171,  171,    0,  171,    0,  171,
  171,  171,  171, 1097,  393,  163,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  156,  157,    0,    0,    0,  158,    0,  159,  160,
    0,  393,    0,    0,    0,    0,    0,  171,  171,    0,
    0,    0,  171,    0,  171,  171,    0,    0,  393,  393,
  393,  393,  393,  393,  393,  393,    0,  393,    0,  393,
  393,  393,  393,    0,    0,    0,    0,    0,    0,    0,
  161,    0,    0,    0,  162,  163,    0,    0,    0,    0,
    0,    0,   42,    0,    0,    0,  171,    0,    0,    0,
  171,  171,    0,    0,    0,   42,   42,  393,  393,   42,
    0,   42,  393,    0,  393,  393,    0,    0,   42,    0,
   42,   42,   42,   42,   42,   42,   42,   42,    0,   42,
   42,   42,    0,  495,    0,  495,    0,    0,    0,    0,
    0,  690,  495,  495,  690,    0,    0,    0,    0,    0,
  690,    0,    0,    0,  690,  690,  393,  690,    0,    0,
  393,  393,    0,    0,    0,  495,    0,  495,  495,  495,
  495,  495,    0,  495,  495,  495,  495,    0,  495,  495,
    0,  495,  495,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  833,    0,    0,  495,  495,  495,  495,
    0,    0,  495,    0,    0,    0,    0,    0,    0,  495,
  495,    0,    0,  495,    0,  495,    0,    0,    0,    0,
    0,    0,    0,    0,  495,    0,  495,  495,  495,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  514,  495,
  514,    0,  495,    0,  495,    0,  690,  514,  514,  690,
    0,    0,    0,    0,    0,  690,    0,   42,    0,  690,
  690,    0,  690,   42,    0,    0,    0,    0,    0,    0,
  514,    0,  514,  514,  514,  514,  514,    0,  514,  514,
  514,  514,    0,  514,  514,    0,  514,  514,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  514,  514,  514,  514,    0,    0,  514,    0,    0,
    0,    0,    0,    0,  514,  514,    0,    0,  514,    0,
  514,    0,    0,    0,    0,    0,    0,    0,    0,  514,
    0,  514,  514,  514,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,  514,    0,    0,  514,    0,  514,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  834,  835,    0,  836,  837,  838,  839,  840,  841,  842,
  843,  844,  845,    0,    0,  690,  690,  690,  846,  847,
  848,  849,  850,    0,    0,  851,  852,    0,  853,  854,
    0,    0,    0,    0,    0,    0,  855,    0,    0,  856,
  857,  858,  859,  860,  861,  862,  863,  864,  865,  866,
  867,  868,  626,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,  627,    0,
    0,    0,    0,  628,    0,    0,    0,    0,    0,  629,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  690,  690,  690,  630,  631,    0,    0,  632,  633,  634,
  635,  636,  637,  638,  639,  640,  641,  642,  643,  644,
    0,  645,  646,  647,  648,  649,  650,  651,  652,  653,
  654,  655,  656,  657,  658,  659,  660,  661,  662,   67,
    0,   68,  663,    0,    0,    0,    0,    0,  366,  367,
    0,    0,    0,    0,    0,    0,    0,    0,    0,  664,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,  368,    0,  369,  370,  371,  372,  373,    0,  374,
  375,  376,  377,    0,  378,  379,    0,  380,  381,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    7,    8,  382,  383,    0,    0,  384,    0,
    0,    0,    0,    0,    0,   11,   12,    0,    0,  385,
    0,  386,    0,    0,    0,    0,   67,    0,   68,    0,
  387,    0,  388,   17,   18,  366,  756,    0,    0,    0,
    0,    0,    0,    0,    0,  389,    0,    0,  390,    0,
  391,    0,    0,    0,    0,    0,    0,    0,  368,    0,
  369,  370,  371,  372,  373,    0,  374,  375,  376,  377,
    0,  378,  379,    0,  380,  381,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    7,
    8,  382,  383,    0,    0,  384,    0,    0,    0,    0,
    0,    0,   11,   12,    0,    0,  385,    0,  386,    0,
    0,    0,    0,  497,    0,  497,    0,  387,    0,  388,
   17,   18,  497,  497,    0,    0,    0,    0,    0,    0,
    0,    0,  389,    0,    0,  390,    0,  391,    0,    0,
    0,    0,    0,    0,    0,  497,    0,  497,  497,  497,
  497,  497,    0,  497,  497,  497,  497,    0,  497,  497,
    0,  497,  497,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,    0,    0,    0,  497,  497,  497,  497,
    0,    0,  497,    0,    0,    0,    0,    0,    0,  497,
  497,    0,    0,  497,    0,  497,    0,    0,    0,    0,
  464,    0,  464,    0,  497,    0,  497,  497,  497,  464,
  464,    0,    0,    0,    0,    0,    0,    0,    0,  497,
    0,    0,  497,    0,  497,    0,    0,    0,    0,    0,
    0,    0,  464,    0,  464,  464,  464,  464,  464,    0,
  464,  464,  464,  464,    0,  464,  464,    0,  464,  464,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
    0,    0,    0,  464,  464,  464,  464,    0,    0,  464,
    0,    0,    0,    0,    0,    0,  464,  464,    0,    0,
  464,    0,  464,    0,    0,    0,    0,  434,    0,  434,
    0,  464,    0,  464,  464,  464,  434,  434,    0,    0,
    0,    0,    0,    0,    0,    0,  464,    0,    0,  464,
    0,  464,    0,    0,    0,    0,    0,    0,    0,  434,
    0,  434,  434,  434,  434,  434,    0,  434,  434,  434,
  434,    0,  434,  434,    0,  434,  434,    0,    0,    0,
    0,    0,    0,    0,    0,    0,    0,    0,    0,    0,
  434,  434,  434,  434,    0,    0,  434,    0,    0,    0,
    0,    0,    0,  434,  434,    0,    0,  434,    0,  434,
    0,    0,    0,    0,  296,    0,  296,    0,  434,    0,
  434,  434,  434,  296,  296,    0,    0,    0,    0,    0,
    0,    0,    0,  434,    0,    0,  434,    0,  434,    0,
    0,    0,    0,    0,    0,    0,  296,    0,  296,  296,
  296,  296,  296,    0,  296,  296,  296,  296,    0,  296,
  296,    0,  296,  296,    0,    0,    0,    0,  327,    0,
    0,    0,    0,    0,    0,    0,    0,  296,  296,  296,
  296,    0,    0,  296,    0,    0,    0,    0,    0,    0,
  296,  296,    0,    0,  296,    0,  296,    0,    0,    0,
    0,    0,    0,    0,    0,  296,    0,  296,  296,  296,
    3,    0,    0,    0,    0,    4,    0,    0,    5,    6,
  296,    7,    8,  296,    0,  296,    0,    0,    9,   10,
    0,    0,    0,    0,   11,   12,    0,    0,    0,    0,
    0,   13,    0,   14,   15,   16,    0,    0,    0,    0,
    0,    0,   17,   18,    0,    0,    0,    0,    0,    0,
    0,   19,   20,    0,    0,    0,    0,    0,   21,
  };
  protected static readonly short [] yyCheck = {             6,
   90,   14,  117,   16,  207,   12,  157,   57,  277,  294,
  279,   59,  442,   20,  460,  461,   66,  530,  802,   91,
  235,  874,  221,  221,  139,  477,  221,  221,  884,  926,
   97,   56,  545,  242,  281,  269,  259,  258,   51,  260,
  261,  266,   19,  242,  242,  243,  161,  242,  242,  268,
   63,  941,  259,   60,  271,   62,  221,   70,  282,  557,
   73,  258,  560,  260,  261,  962,  258,  267,  260,  261,
  377,  217,  258,  266,  260,  261,  264,  242,  243,  244,
  245,  246,  258,  263,  260,  261,  273,   94,  267,  260,
  267,  269,  140,  263,  240,  273,  269,  263,  213,  272,
  273,  288,  280,  274,  270,  118,  156,  280,  258,  259,
  260,  261,  267,  263,  284,  377,  271,  268,  284,  269,
  170,  171,  263,  264,  371,  458,  459, 1024,  370,  263,
  271,  324,  247,  140,  258,  258,  260,  260,  261,  263,
 1014,  258,  475,  260,  261,  370,  269,  371,  263,  295,
  175,  259,  272,  272,  518,  180,  371,  266,  588,  283,
  283,  281,  281,  278,   99,  100,  101,  102,  103,  104,
  278,  258,  323,  260,  261,  269,  224,  271,  287,  186,
  389,  275,  273,  263,  259,  279,  280,  271,  282,  259,
  281,  273,  263,  287,  242,  480,  209,  281,  271,  270,
  392,  393,  286,  278,  294,  320,  288,  270,  278, 1106,
  281,  453,  454,  258,  271,  260,  261,  265,  225,  282,
  227,  278,  229,  295,  269,  371,  418,  450,  451,  452,
  453,  454,  455,  456,  279,  280,  320,  460,  461,  462,
  463,  331,  421,  420,  421,  317,  273, 1137,  263,  264,
  271,  276,  270,  560,  231,  269,  343,  278,  265,  757,
  377,  288, 1159,  281,  312,  507,  508,  455,  456,  314,
  504,  451,  452,  453,  287,  320, 1150,  273,  268,  545,
  272,  294,  505,  506,  351,  375,  509, 1143,  378,  281,
  362,  363,  288,  318,  511, 1192,  258,  387,  260,  260,
  519,  520,  509, 1156,  376,  267,  484, 1204,  268,  460,
  461,  484,    0,  273,  455,  456,  537,  517,  280,  604,
  310,  369,  269,  546,  372,  332,  259,  377,  335, 1203,
  445,  321,  380,  381,  267,  407, 1120,  269,  271,  329,
  272,  389,  387,  415,  553,  542,  555,  279,  463,  374,
  258,  267,  260,  261,  553,  553,  542,  787,  553,  553,
  270,  537,  369, 1147,  479,  372,  558,  482,  258,  267,
  260,  261,  282,  380,  381,  382,  499,  269,  272,  386,
  272,  527,  389,  390,  270,  279,  267,  279,  553,  902,
  903,  904,  905,  370,  371,  267,  282,  268,  911,  912,
  913,  491,  409,  553,  554,  282,  500,  501,  502,  268,
  287,  418,  425,  267,  273,  499,  562,  430,  564,  267,
  566,  567,  707,  569,  570,  877,  451,  269,  551,  258,
  272,  260,  261,  267,  263,  525,  267,  279,  269,  310,
  455,  456,  258,  491,  260,  261,  494,  263,  279,  280,
  321,  561,    0,  343,  270,  500,  501,  502,  329,  270,
  575,  560,  577,  547,  266,  273,  751,  480,  284,  271,
  495,  282,  269,  268,  620,  272,  591,  525,  272,  273,
  530,  596,  279,  280,  392,  393,  266,  535,  270,  451,
  452,  453,  454,  455,  456,  545,  262,  263,  523,  269,
  282,  271,  269,  271,  529,  553,  531,  555,  269,  279,
  280,  272,  279,  280,  343,  714,  715,  542,  279,  714,
  715,  269,  668,  792,  272,  269,  321,  343,  535,  554,
  258,  279,  260,  261,  541,  279,  280,  332,  586,  279,
  547,  692,  337,  505,  506,  279,  697,  509,  555,  714,
  715,  258,  282,  260,  561,  580,  263,  262,  263,  572,
  573,  356,  357,  258,  278,  260,  501,  502,  503,  504,
  365,  499,  507,  508,  269, 1021,  691,  258,  269,  260,
  268,  272,  258,  270,  260,  261,  789,  733,  279,  277,
  597,  604,  807,  808,  271,  272,  273,  258,  670,  260,
  261,  260,  258,  270,  260,  261,  273,  278,  269,  278,
  266,  267,  281,  269,  258,  410,  260,  261,  666,  269,
  692,  271,  310,  279,  280,  269,  282,  315,  743,  270,
  318,  319,  273,  321,  322,  279,  280,  325,  314,  271,
  328,  329,    0,  269,  320,  271,  334,  335,  738,  269,
  698,  281,  272,  341,  923,  343,  344,  345,  271,  279,
  280,  349,  350,  351,  352,  353,  269,  355,  271,  500,
  501,  502,  360,  361,  362,  451,  452,  453,  454,  270,
  368,  258,  273,  260,  261,  692,  517,  258,  270,  260,
  738,  273,  269,  706,  707,  269,  744,  271,  269,  475,
  713,  708,  270,  500,  501,  502,  754,  271,  279,  280,
  269,  736,  271,  761,  269,  271,  271,  730,  271,  280,
  268,  420,  747,  748,  749,  451,  452,  453,  454,  272,
  500,  501,  502,  500,  501,  502,  743,  475,  751,  378,
  379,  380,  381,  382,  444,  445,  753, 1032,  755,  475,
  287,  269,  824,  271,  761,  271,  500,  501,  502,  272,
  273,  964,  310,  770,  258,  790,  260,  315,  775,  271,
  318,  319,  779,  321,  322,  800,  451,  452,  453,  454,
  328,  329,  272,  273,  332,  271,  334,  335,  813,  383,
  384,  385,  386,  341,  801,  343,  344,  345,  332,  392,
  393,  394,  874, 1088,  352,  353,  399,  400,  401,  273,
  258, 1014,  260,  361,  362,  263,  387,  272,  273,  267,
  368,  513,  514,  515,  516, 1110,  803,  804,  805,  806,
  807,  808,  809,  810,  811,  812,  260,  814,  815,  259,
  383,  384,  385,  386,  500,  501,  502,  272,  273,  939,
  259, 1054,  267,  999,  268,  504,  500,  501,  502,  553,
  554,  519,  520,  870,  258,  267,  260,  261,  267,  412,
  413,  414,  415,  556,  557,  269,  924,  272,  280,  272,
  500,  501,  502,  931,    0,  428,  272,  430,  431,  937,
  451,  452,  453,  454,  455,  456,  272,  258,  272,  260,
  261, 1186,  271,  951,  556,  557,  271,  321,  272,  273,
  268,  936,  272,  273,  927,  272,  273,  965,  332,  272,
  273,  993,  272,  337,  931,  272,  939,  265,  953,  500,
  501,  502,  957, 1025, 1026, 1027, 1028,  272,  273,  272,
  273,  271,  356,  357,  505,  506, 1097, 1150,  509,  272,
  273,  365,  310,  272,  273,  272,  273,  315,    0,  269,
  318,  319,  274,  321,  322,  272,  273,  272,  273,  538,
  328,  329, 1022,  272,  272,  273,  334,  335,  985,  986,
  272,  273,  343,  341,  278,  343,  344,  345, 1036,  278,
  997, 1039,  272,  273,  352,  353,  410,  272,  273, 1047,
 1203,  272,  273,  361,  362, 1053, 1078,  377, 1015, 1016,
  368,  272,  273,  278,  278,  258,  559,  260,  377, 1032,
  421,  377,  284,  275,  271,  370,  269,  388,  389,  390,
  268,  260, 1057,  278,  278,  275,  279,  280,  432, 1046,
  272, 1048, 1049,  271,  275,  512, 1136,  268,  274,  451,
  452,  453,  454,  455,  456,  449,  450,  451,  452,  453,
  454,  455,  456,  512,  458,  274,  460,  461,  462,  463,
  274, 1119,  310, 1188, 1099, 1088,  273,  315, 1085,  271,
  268,  272,  272,  321,  383,  384,  385,  386,  284,  310,
  271,  329,  271,  271,  315,  271,  271, 1110,  271,  271,
  321,  271,  271,  505,  506,  271,  271,  509,  329,  503,
  271,  505,  506,  412,  413,  414,  415,  271,  392,  393,
  394,  271, 1129, 1130, 1131,  399,  400,  401,  479,  428,
  466,  430,  431,  473, 1141, 1142, 1161,  271,  278,  450,
  270, 1189,  258,  274,  260,  261,  274,  270,  270,  287,
    0,  267,  268,  269,  270,  271,  272,  273,  552,  275,
  271,  267,  278,  279,  280,  272,  282,  283,  284,  272,
  272,  287,  288, 1186,  290,  272,  292,  293,  294,  295,
  296, 1188,  298,  299,  300,  301,  275,  303,  304,  270,
  306,  307,  270,  284,  310,  271,  260,  268,  314,  315,
  269,  271,  318,  319,  320,  321,  322,  323,  324,  325,
  269,  327,  328,  329,  277,  324,  332,  271,  334,  335,
  271,  337,  338,  271,  340,  341,  268,  343,  344,  345,
  270,  510,  275,  349,  350,  351,  352,  353,  275,  355,
  356,  270,  371,  512,  360,  361,  362,  274,  364,  365,
  321,  367,  368,  369,  370,  371,  512,  500,  501,  502,
  559,  332,  274,  272,  284,  273,  337,  270,  310,  270,
  272,  387,  272,  315,  272,  272,  318,  319,  272,  321,
  322,  352,  353,  325,  272,  356,  328,  329,  272,  405,
  272,  272,  334,  335,  365,  272,  272,  272,  272,  341,
  268,  343,  344,  345,  420,  421,  272,  349,  350,  351,
  352,  353,  268,  355,  272,  272,  432,  271,  360,  361,
  362,  271,  275,  270,  270,  270,  368,  260,  272,  272,
  270,  272,  274,  449,  450,  451,  452,  453,  454,  455,
  456,  267,  458,  274,  460,  461,  462,  463,  271,  258,
  274,  260,  261,  321,  280,  274,  274,  271,  278,  271,
  269,  272,  287,  331,  272,  321,  334,  335,  272,  272,
  279,  280,  272,  275,  283,  271,  332,  260,  270,  272,
  348,  337,  498,  499,  500,  501,  502,  503,  273,  505,
  506,  359,  271,  288,  510,  271,  352,  353,  272,    0,
  356,  517,  267,  267,  273,  268,  267,  267,  258,  365,
  260,  261,  267,  272,  267,  272,    0,  267,  268,  269,
  270,  271,  272,  273,  268,  275,  267,  267,  278,  279,
  280,  547,  282,  283,  268,  551,  552,  287,  288,  271,
  290,  267,  292,  293,  294,  295,  296,  310,  298,  299,
  300,  301,  315,  303,  304,  271,  306,  307,  321,  272,
  310,  267,  272,  267,  314,  315,  329,  267,  318,  319,
  320,  321,  322,  323,  324,  325,  271,  327,  328,  329,
   95,  221,  332,  221,  334,  335,  221,  337,  338,  690,
  340,  341,  578,  343,  344,  345,  405,  781,  951,  349,
  350,  351,  352,  353,  221,  355,  356,  382, 1156,  461,
  360,  361,  362,  403,  364,  365,  599,  367,  368,  369,
 1190,  371, 1054,  432, 1119,  451,  452,  453,  454,  455,
  456,  748,  936,  558,  268,   18,  624,  387,  697, 1019,
  449,  450,  451,  452,  453,  454,  455,  456, 1133,  458,
  694,  460,  461,  462,  463,  405,  876,  432,  433,  434,
  435,  436,  437,  438,  439,  440,  441,   47,  115,   -1,
   -1,  421,   -1,   -1,   -1,  514,  310,   -1,   -1,  505,
  506,  315,  432,  509,   -1,   -1,   -1,  321,   -1,  498,
  499,  500,  501,  502,  503,  329,  505,  506,   -1,  449,
  450,  451,  452,  453,  454,  455,  456,   -1,  458,   -1,
  460,  461,  462,  463,   -1,  258,   -1,  260,  261,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  269,   -1,   -1,  272,
  273,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  547,   -1,
  283,   -1,  551,  552,   -1,   -1,   -1,   -1,  498,  499,
  500,  501,  502,  503,   -1,  505,  506,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  517,  543,   -1,
   -1,   -1,   -1,   -1,  258,   -1,  260,  261,   -1,   -1,
   -1,   -1,    0,  267,  268,  269,  270,  271,  272,  273,
   -1,  275,   -1,   -1,  278,  279,  280,  547,  282,  283,
   -1,  551,  552,  287,  288,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,   -1,   -1,  310,   -1,   -1,   -1,
  314,  315,   -1,   -1,  318,  319,  320,  321,  322,  323,
  324,  325,   -1,  327,  328,  329,   -1,   -1,  332,   -1,
  334,  335,   -1,  337,  338,   -1,  340,  341,   -1,  343,
  344,  345,  405,   -1,   -1,  349,  350,  351,  352,  353,
   -1,  355,  356,   -1,   -1,   -1,  360,  361,  362,   -1,
  364,  365,   -1,  367,  368,  369,   -1,  371,   -1,  432,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  387,   -1,   -1,  449,  450,  451,  452,
  453,  454,  455,  456,   -1,  458,   -1,  460,  461,  462,
  463,  405,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  450,  451,  452,  453,  454,  455,  456,  421,   -1,   -1,
  460,  461,  462,  463,   -1,   -1,   -1,   -1,  432,   -1,
   -1,   -1,   -1,   -1,   -1,  498,  499,   -1,   -1,   -1,
  503,   -1,  505,  506,   -1,  449,  450,  451,  452,  453,
  454,  455,  456,   -1,  458,   -1,  460,  461,  462,  463,
   -1,  258,   -1,  260,  261,  505,  506,   -1,   -1,  509,
   -1,   -1,  269,   -1,  271,   -1,  273,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  547,   -1,  283,   -1,  551,  552,
   -1,   -1,   -1,   -1,  498,  499,  500,  501,  502,  503,
   -1,  505,  506,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  552,  517,   -1,   -1,   -1,   -1,   -1,   -1,
  258,   -1,  260,  261,   -1,   -1,   -1,   -1,    0,  267,
  268,  269,  272,  271,  272,  273,   -1,  275,   -1,   -1,
  278,  279,  280,  547,   -1,  283,   -1,  551,  552,  287,
  288,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
   -1,   -1,  310,   -1,   -1,   -1,  314,  315,   -1,   -1,
  318,  319,  320,  321,  322,  323,  324,  325,   -1,  327,
  328,  329,   -1,   -1,  332,   -1,  334,  335,   -1,  337,
  338,   -1,  340,  341,   -1,  343,  344,  345,  405,   -1,
   -1,  349,  350,  351,  352,  353,   -1,  355,  356,   -1,
   -1,   -1,  360,  361,  362,   -1,  364,  365,   -1,  367,
  368,  369,   -1,   -1,   -1,  432,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  383,  384,  385,  386,   -1,   -1,  387,
   -1,   -1,  449,  450,  451,  452,  453,  454,  455,  456,
   -1,  458,   -1,  460,  461,  462,  463,  405,   -1,   -1,
   -1,   -1,  412,  413,  414,  415,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  421,   -1,   -1,   -1,   -1,  428,   -1,
  430,  431,   -1,   -1,  432,   -1,   -1,   -1,   -1,   -1,
   -1,  498,  499,   -1,   -1,   -1,  503,   -1,  505,  506,
   -1,  449,  450,  451,  452,  453,  454,  455,  456,   -1,
  458,   -1,  460,  461,  462,  463,   -1,  258,   -1,  260,
  261,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  269,   -1,
   -1,  272,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  547,   -1,  283,   -1,  551,  552,   -1,   -1,   -1,   -1,
  498,  499,  500,  501,  502,  503,   -1,  505,  506,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  517,
   -1,   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,  261,
   -1,   -1,   -1,   -1,    0,  267,  268,  269,  272,  271,
  272,  273,   -1,  275,   -1,   -1,  278,  279,  280,  547,
   -1,  283,   -1,  551,  552,  287,  288,   -1,  290,  559,
  292,  293,  294,  295,  296,   -1,  298,  299,  300,  301,
   -1,  303,  304,   -1,  306,  307,   -1,   -1,  310,   -1,
   -1,   -1,  314,  315,   -1,   -1,  318,  319,  320,  321,
  322,  323,  324,  325,   -1,  327,  328,  329,   -1,   -1,
  332,   -1,  334,  335,   -1,  337,  338,   -1,  340,  341,
   -1,  343,  344,  345,  405,   -1,   -1,  349,  350,  351,
  352,  353,   -1,  355,  356,   -1,   -1,   -1,  360,  361,
  362,   -1,  364,  365,   -1,  367,  368,  369,   -1,   -1,
   -1,  432,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  383,
  384,  385,  386,   -1,   -1,  387,   -1,   -1,  449,  450,
  451,  452,  453,  454,  455,  456,   -1,  458,   -1,  460,
  461,  462,  463,  405,   -1,   -1,   -1,   -1,  412,  413,
  414,  415,   -1,   -1,   -1,  268,   -1,   -1,   -1,  421,
   -1,   -1,   -1,   -1,  428,   -1,  430,  431,   -1,   -1,
  432,   -1,   -1,   -1,   -1,   -1,   -1,  498,  499,   -1,
  268,   -1,  503,   -1,  505,  506,   -1,  449,  450,  451,
  452,  453,  454,  455,  456,  308,  458,   -1,  460,  461,
  462,  463,   -1,  258,   -1,  260,  261,   -1,  321,   -1,
   -1,   -1,   -1,   -1,  269,   -1,   -1,  330,   -1,   -1,
  308,  334,  335,   -1,   -1,   -1,  547,   -1,  283,   -1,
  551,  552,   -1,  321,   -1,  348,  498,  499,  500,  501,
  502,  503,  330,  505,  506,  358,  334,  335,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  517,   -1,   -1,   -1,   -1,
  348,   -1,  258,   -1,  260,  261,   -1,   -1,   -1,    0,
  358,  267,  268,  269,   -1,  271,  272,  273,   -1,  275,
   -1,   -1,  278,  279,  280,  547,   -1,  283,   -1,  551,
  552,  287,  288,   -1,  290,  559,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,   -1,   -1,  310,   -1,   -1,   -1,  314,  315,
   -1,   -1,  318,  319,  320,  321,  322,  323,  324,  325,
   -1,  327,  328,  329,   -1,   -1,  332,   -1,  334,  335,
   -1,  337,  338,    0,  340,  341,   -1,  343,  344,  345,
  405,   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,
  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,  365,
   -1,  367,  368,  369,   -1,   -1,   -1,  432,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  387,   -1,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,  405,
   -1,   -1,   -1,   -1,   -1,   -1,  450,  451,  452,  453,
  454,  455,  456,   -1,   -1,  421,  460,  461,  462,  463,
   -1,   -1,   -1,   -1,   -1,   -1,  432,   -1,   -1,   -1,
   -1,   -1,   -1,  498,  499,   -1,   -1,   -1,  503,   -1,
  505,  506,   -1,  449,  450,  451,  452,  453,  454,  455,
  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,   -1,
  504,  505,  506,   -1,   -1,  509,   -1,  450,  451,  452,
  453,  454,  455,  456,   -1,   -1,   -1,  460,  461,  462,
  463,   -1,  547,   -1,   -1,   -1,  551,  552,   -1,   -1,
   -1,   -1,  498,  499,  500,  501,  502,  503,   -1,  505,
  506,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  552,   -1,
   -1,  517,   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,
  261,  504,  505,  506,    0,   -1,  267,  268,  269,   -1,
  271,  272,  273,   -1,  275,   -1,   -1,   -1,  279,  280,
   -1,  547,  283,   -1,   -1,  551,  552,  288,   -1,  290,
   -1,  292,  293,  294,  295,  296,   -1,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,  310,
   -1,   -1,   -1,  314,  315,   -1,   -1,  318,  319,  320,
  321,  322,  323,  324,  325,   -1,  327,  328,  329,   -1,
   -1,  332,   -1,  334,  335,   -1,  337,  338,    0,  340,
  341,  268,  343,  344,  345,   -1,   -1,   -1,  349,  350,
  351,  352,  353,   -1,  355,  356,   -1,   -1,   -1,  360,
  361,  362,   -1,  364,  365,   -1,  367,  368,  369,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  310,   -1,   -1,  387,   -1,  315,   -1,
   -1,  318,  319,   -1,  321,  322,   -1,   -1,   -1,   -1,
   -1,  328,  329,   -1,  405,   -1,   -1,  334,  335,   -1,
   -1,   -1,   -1,   -1,  341,   -1,  343,  344,  345,   -1,
  421,   -1,   -1,   -1,   -1,  352,  353,   -1,   -1,   -1,
   -1,  432,   -1,   -1,  361,  362,   -1,   -1,   -1,   -1,
   -1,  368,   -1,   -1,   -1,   -1,   -1,   -1,  449,  450,
  451,  452,  453,  454,  455,  456,   -1,  458,   -1,  460,
  461,  462,  463,   -1,  378,  379,  380,  381,  382,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  391,  392,  393,
  394,   -1,  396,  397,  398,  399,  400,  401,  402,   -1,
   -1,   -1,   -1,   -1,   -1,  409,   -1,  498,  499,  500,
  501,  502,  503,   -1,  505,  506,   -1,   -1,  422,  423,
  424,  425,  426,  427,   -1,   -1,  517,   -1,   -1,   -1,
   -1,   -1,  258,   -1,  260,  261,   -1,    0,   -1,   -1,
   -1,  267,  268,  269,   -1,  271,  272,  273,   -1,  275,
   -1,   -1,   -1,  279,  280,   -1,  547,  283,   -1,   -1,
  551,  552,  288,   -1,  290,   -1,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,   -1,   -1,  310,   -1,   -1,   -1,  314,  315,
   -1,   -1,  318,  319,  320,  321,  322,  323,  324,  325,
   -1,  327,  328,  329,   -1,   -1,  332,   -1,  334,  335,
   -1,  337,  338,   -1,  340,  341,  268,  343,  344,  345,
   -1,   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,
  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,  365,
   -1,  367,  368,  369,  548,  549,   -1,   -1,   -1,   -1,
    0,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  310,   -1,
   -1,  387,   -1,  315,   -1,   -1,  318,  319,   -1,  321,
  322,   -1,   -1,   -1,   -1,   -1,  328,  329,   -1,  405,
   -1,   -1,  334,  335,   -1,   -1,   -1,   -1,   -1,  341,
   -1,  343,  344,  345,   -1,   -1,   -1,   -1,   -1,   -1,
  352,  353,   -1,   -1,   -1,   -1,  432,   -1,   -1,  361,
  362,   -1,   -1,   -1,   -1,   -1,  368,   -1,   -1,   -1,
   -1,   -1,   -1,  449,  450,  451,  452,  453,  454,  455,
  456,   -1,  458,   -1,  460,  461,  462,  463,  521,  522,
  523,  524,  525,  526,  527,  528,  529,  530,  531,  532,
  533,  534,  535,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  498,  499,  500,  501,  502,  503,   -1,  505,
  506,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  517,   -1,   -1,   -1,  258,   -1,  260,   -1,   -1,
   -1,   -1,   -1,   -1,  267,  268,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  547,   -1,   -1,   -1,  551,  552,  290,   -1,  292,
  293,  294,  295,  296,   -1,  298,  299,  300,  301,   -1,
  303,  304,   -1,  306,  307,  308,   -1,  310,   -1,   -1,
   -1,   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,
  323,  324,  325,   -1,  327,  328,  329,  330,  331,  332,
   -1,  334,  335,   -1,  337,  338,    0,  340,  341,   -1,
  343,  344,  345,   -1,   -1,  348,  349,  350,  351,  352,
  353,   -1,  355,  356,  357,  358,  359,  360,  361,  362,
   -1,  364,  365,   -1,  367,  368,  369,   -1,  258,   -1,
  260,   -1,   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  290,   -1,  292,  293,  294,  295,  296,  410,  298,  299,
  300,  301,   -1,  303,  304,   -1,  306,  307,  308,   -1,
  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,
   -1,  321,  322,  323,  324,  325,   -1,  327,  328,  329,
  330,  331,  332,   -1,  334,  335,   -1,  337,  338,    0,
  340,  341,   -1,  343,  344,  345,   -1,   -1,  348,  349,
  350,  351,  352,  353,   -1,  355,  356,  357,  358,  359,
  360,  361,  362,   -1,  364,  365,   -1,  367,  368,  369,
   -1,  258,   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,
  267,  268,   -1,   -1,  271,   -1,   -1,   -1,  275,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,
  410,  298,  299,  300,  301,   -1,  303,  304,   -1,  306,
  307,   -1,   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,
   -1,  318,  319,   -1,  321,  322,  323,  324,  325,   -1,
  327,  328,  329,   -1,    0,  332,   -1,  334,  335,   -1,
  337,  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,
   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,  356,
   -1,   -1,   -1,  360,  361,  362,   -1,  364,  365,   -1,
  367,  368,  369,   -1,  258,   -1,  260,   -1,   -1,   -1,
   -1,   -1,   -1,  267,  268,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,  308,   -1,  310,   -1,   -1,   -1,
   -1,  315,    0,   -1,  318,  319,   -1,  321,  322,  323,
  324,  325,   -1,  327,  328,  329,  330,  331,   -1,   -1,
  334,  335,   -1,   -1,  338,   -1,  340,  341,   -1,  343,
  344,  345,   -1,   -1,  348,  349,  350,  351,  352,  353,
   -1,  355,   -1,   -1,  358,  359,  360,  361,  362,   -1,
  364,   -1,   -1,  367,  368,  369,   -1,  258,   -1,  260,
   -1,   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,
   -1,  292,  293,  294,  295,  296,   -1,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,  308,   -1,  310,
   -1,   -1,   -1,   -1,  315,    0,   -1,  318,  319,   -1,
  321,  322,  323,  324,  325,   -1,  327,  328,  329,  330,
  331,   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,
  341,   -1,  343,  344,  345,   -1,   -1,  348,  349,  350,
  351,  352,  353,   -1,  355,   -1,   -1,  358,  359,  360,
  361,  362,  258,  364,   -1,   -1,  367,  368,  369,   -1,
   -1,  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,  308,   -1,  310,   -1,   -1,   -1,   -1,  315,
   -1,   -1,  318,  319,   -1,  321,  322,  323,  324,  325,
    0,  327,  328,  329,  330,  331,   -1,   -1,  334,  335,
   -1,   -1,  338,   -1,  340,  341,   -1,  343,  344,  345,
   -1,   -1,  348,  349,  350,  351,  352,  353,   -1,  355,
  258,   -1,  358,  359,  360,  361,  362,   -1,  364,  267,
  268,  367,  368,  369,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
  308,   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,
  318,  319,   -1,  321,  322,  323,  324,  325,   -1,  327,
  328,  329,  330,  331,    0,   -1,  334,  335,   -1,   -1,
  338,   -1,  340,  341,   -1,  343,  344,  345,   -1,   -1,
  348,  349,  350,  351,  352,  353,   -1,  355,   -1,   -1,
  358,  359,  360,  361,  362,   -1,  364,   -1,   -1,  367,
  368,  369,   -1,  258,   -1,  260,   -1,   -1,   -1,   -1,
   -1,   -1,  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,  310,   -1,   -1,   -1,   -1,
  315,   -1,   -1,  318,  319,   -1,  321,  322,  323,  324,
  325,    0,  327,  328,  329,   -1,   -1,  332,   -1,  334,
  335,   -1,  337,  338,   -1,  340,  341,   -1,  343,  344,
  345,   -1,   -1,   -1,  349,  350,  351,  352,  353,   -1,
  355,  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,
  365,   -1,  367,  368,  369,   -1,   -1,   -1,  258,   -1,
  260,   -1,   -1,   -1,   -1,   -1,   -1,  267,  268,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  290,   -1,  292,  293,  294,  295,  296,   -1,  298,  299,
  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,
  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,
   -1,  321,  322,  323,  324,  325,    0,  327,  328,  329,
   -1,   -1,  332,   -1,  334,  335,   -1,  337,  338,   -1,
  340,  341,   -1,  343,  344,  345,   -1,   -1,   -1,  349,
  350,  351,  352,  353,   -1,  355,  356,   -1,   -1,   -1,
  360,  361,  362,   -1,  364,  365,   -1,  367,  368,  369,
   -1,  267,  268,   -1,   -1,  271,   -1,   -1,   -1,  275,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,   -1,   -1,  310,   -1,   -1,   -1,   -1,  315,
   -1,   -1,  318,  319,    0,  321,  322,  323,  324,  325,
   -1,  327,  328,  329,   -1,   -1,  332,   -1,  334,  335,
   -1,  337,  338,   -1,  340,  341,   -1,  343,  344,  345,
   -1,   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,
  356,   -1,   -1,   -1,  360,  361,  362,   -1,  364,  365,
   -1,  367,  368,  369,   -1,   -1,   -1,   -1,  267,  268,
   -1,   -1,  271,   -1,   -1,   -1,  275,   -1,   -1,   -1,
   -1,   -1,   -1,    0,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,
   -1,  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,
  319,   -1,  321,  322,  323,  324,  325,   -1,  327,  328,
  329,    0,   -1,  332,   -1,  334,  335,   -1,  337,  338,
   -1,  340,  341,   -1,  343,  344,  345,   -1,   -1,   -1,
  349,  350,  351,  352,  353,   -1,  355,  356,   -1,   -1,
   -1,  360,  361,  362,   -1,  364,  365,   -1,  367,  368,
  369,   -1,   -1,   -1,  258,   -1,  260,   -1,   -1,    0,
   -1,   -1,   -1,  267,  268,   -1,   -1,   -1,   -1,  273,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,   -1,   -1,  310,   -1,   -1,   -1,
   -1,  315,   -1,   -1,  318,  319,   -1,  321,  322,  323,
  324,  325,   -1,  327,  328,  329,   -1,   -1,   -1,   -1,
  334,  335,   -1,   -1,  338,   -1,  340,  341,   -1,  343,
  344,  345,   -1,   -1,   -1,  349,  350,  351,  352,  353,
   -1,  355,  258,   -1,  260,   -1,  360,  361,  362,   -1,
  364,  267,  268,  367,  368,  369,   -1,   -1,   -1,   -1,
   -1,  277,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,  295,
  296,   -1,  298,  299,  300,  301,   -1,  303,  304,   -1,
  306,  307,   -1,   -1,  310,   -1,   -1,   -1,   -1,  315,
   -1,   -1,  318,  319,   -1,  321,  322,  323,  324,  325,
   -1,  327,  328,  329,   -1,   -1,   -1,   -1,  334,  335,
   -1,  268,  338,   -1,  340,  341,   -1,  343,  344,  345,
  277,  278,   -1,  349,  350,  351,  352,  353,   -1,  355,
   -1,   -1,   -1,   -1,  360,  361,  362,   -1,  364,   -1,
   -1,  367,  368,  369,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  310,   -1,   -1,   -1,   -1,  315,  268,
   -1,  318,  319,   -1,  321,  322,   -1,   -1,  325,   -1,
   -1,  328,  329,   -1,   -1,   -1,   -1,  334,  335,   -1,
   -1,   -1,   -1,   -1,  341,   -1,  343,  344,  345,   -1,
   -1,   -1,  349,  350,  351,  352,  353,   -1,  355,   -1,
   -1,  310,   -1,  360,  361,  362,  315,  268,   -1,  318,
  319,  368,  321,  322,   -1,   -1,   -1,   -1,   -1,  328,
  329,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,   -1,
   -1,   -1,  341,   -1,  343,  344,  345,   -1,   -1,   -1,
   -1,   -1,   -1,  352,  353,   -1,   -1,   -1,   -1,  310,
   -1,   -1,  361,  362,  315,   -1,   -1,  318,  319,  368,
  321,  322,   -1,   -1,   -1,   -1,   -1,  328,  329,   -1,
   -1,   -1,   -1,  334,  335,   -1,   -1,   -1,   -1,   -1,
  341,   -1,  343,  344,  345,   -1,   -1,   -1,   -1,   -1,
   -1,  352,  353,  258,   -1,  260,  261,   -1,   -1,   -1,
  361,  362,  267,  268,  269,   -1,  271,  368,  273,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  283,  284,
   -1,   -1,  287,   -1,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  321,  322,  323,  324,
   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,   -1,  334,
  335,   -1,   -1,  338,   -1,  340,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  349,   -1,  351,  352,  353,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  364,
   -1,   -1,  367,   -1,  369,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  258,   -1,  260,  261,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  269,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  283,
  405,   -1,   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,
  261,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  269,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  432,  279,  280,
   -1,   -1,  283,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,
   -1,   -1,   -1,  314,   -1,   -1,   -1,   -1,   -1,  320,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  498,  499,   -1,   -1,   -1,  503,   -1,
  505,  506,   -1,  387,   -1,  510,   -1,  391,  392,  393,
  394,  395,  396,   -1,  398,  399,  400,  401,  402,  403,
  404,  405,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  258,   -1,  260,  261,
   -1,   -1,  547,   -1,   -1,   -1,  551,  552,  432,   -1,
   -1,   -1,   -1,   -1,  405,   -1,   -1,   -1,   -1,   -1,
   -1,  283,   -1,   -1,   -1,  449,  450,  451,  452,  453,
  454,  455,  456,   -1,  458,   -1,  460,  461,  462,  463,
   -1,  432,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  449,  450,
  451,  452,  453,  454,  455,  456,   -1,  458,   -1,  460,
  461,  462,  463,   -1,  498,  499,   -1,   -1,   -1,  503,
   -1,  505,  506,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  498,  499,  500,
  501,  502,  503,   -1,  505,  506,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  547,   -1,  387,   -1,  551,  552,  391,
  392,  393,  394,  395,  396,   -1,  398,  399,  400,  401,
  402,  403,  404,  405,   -1,   -1,  258,   -1,  260,  261,
   -1,   -1,   -1,   -1,   -1,   -1,  547,  269,   -1,   -1,
  551,  552,   -1,   -1,   -1,   -1,   -1,  279,  280,   -1,
  432,  283,   -1,   -1,  258,   -1,  260,  261,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  449,  450,  451,
  452,  453,  454,  455,  456,   -1,  458,   -1,  460,  461,
  462,  463,  314,   -1,   -1,   -1,   -1,  258,  320,  260,
  261,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  269,   -1,
   -1,   -1,   -1,  258,   -1,  260,  261,   -1,  279,  280,
   -1,   -1,  283,   -1,  269,   -1,  498,  499,   -1,   -1,
   -1,  503,   -1,  505,  506,   -1,   -1,   -1,  283,  310,
   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,   -1,
  321,  322,   -1,  314,   -1,   -1,   -1,  328,  329,  320,
   -1,   -1,   -1,  334,  335,   -1,   -1,   -1,   -1,   -1,
  341,   -1,  343,  344,  345,  547,   -1,   -1,   -1,  551,
  552,  352,  353,  405,   -1,  379,   -1,   -1,   -1,   -1,
  361,  362,   -1,   -1,   -1,   -1,   -1,  368,  392,  393,
   -1,   -1,  396,   -1,  398,   -1,   -1,   -1,   -1,   -1,
  432,  405,   -1,  407,  408,  409,  410,  411,  412,  413,
  414,   -1,  416,  417,  418,   -1,   -1,  449,  450,  451,
  452,  453,  454,  455,  456,   -1,  458,   -1,  460,  461,
  462,  463,   -1,   -1,  405,  258,   -1,  260,  261,   -1,
   -1,  396,   -1,  398,   -1,   -1,  269,   -1,   -1,   -1,
  405,  258,   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,
  283,  432,  269,   -1,   -1,   -1,  498,  499,  500,  501,
  502,  503,   -1,  505,  506,   -1,  283,  432,  449,  450,
  451,  452,  453,  454,  455,  456,   -1,  458,   -1,  460,
  461,  462,  463,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,
   -1,  258,   -1,  260,  261,  547,   -1,   -1,   -1,  551,
  552,   -1,  269,   -1,   -1,   -1,   -1,  498,  499,  500,
  501,  502,  503,   -1,  505,  506,  283,   -1,   -1,   -1,
  544,   -1,   -1,  498,  499,   -1,  550,   -1,  503,   -1,
  505,  506,   -1,   -1,   -1,  378,  379,  380,  381,  382,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  378,   -1,   -1,   -1,   -1,  547,   -1,   -1,   -1,
  551,  552,  405,  258,   -1,  260,  261,   -1,   -1,  396,
   -1,  398,  547,   -1,  269,   -1,  551,  552,  405,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  283,  432,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  432,  449,  450,  451,  452,
  453,  454,  455,  456,   -1,  458,   -1,  460,  461,  462,
  463,   -1,  449,  450,  451,  452,  453,  454,  455,  456,
   -1,  458,   -1,  460,  461,  462,  463,   -1,  405,  258,
   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  269,   -1,   -1,   -1,   -1,  498,  499,   -1,   -1,   -1,
  503,   -1,  505,  506,  283,  432,   -1,   -1,   -1,   -1,
   -1,  498,  499,   -1,   -1,   -1,  503,   -1,  505,  506,
   -1,   -1,  449,  450,  451,  452,  453,  454,  455,  456,
   -1,  458,   -1,  460,  461,  462,  463,   -1,   -1,   -1,
   -1,   -1,   -1,  258,  547,  260,  261,   -1,  551,  552,
  405,   -1,   -1,   -1,  269,   -1,   -1,   -1,   -1,  258,
  547,  260,  261,   -1,  551,  552,   -1,   -1,  283,   -1,
  269,  498,  499,   -1,  268,   -1,  503,  432,  505,  506,
   -1,   -1,   -1,   -1,  283,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  519,   -1,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  547,  315,   -1,   -1,  551,  552,  405,  321,  322,   -1,
   -1,  325,   -1,   -1,  328,   -1,   -1,   -1,   -1,   -1,
  334,  335,   -1,  498,  499,   -1,   -1,  341,  503,   -1,
  505,  506,   -1,  432,   -1,  349,  350,  351,  352,  353,
   -1,  355,   -1,   -1,   -1,   -1,  360,   -1,   -1,   -1,
  449,  450,  451,  452,  453,  454,  455,  456,   -1,  458,
   -1,  460,  461,  462,  463,   -1,   -1,   -1,   -1,   -1,
  405,  258,  547,  260,  261,   -1,  551,  552,   -1,   -1,
   -1,   -1,  269,   -1,   -1,   -1,  405,  258,   -1,  260,
  261,   -1,   -1,   -1,   -1,   -1,  283,  432,  269,  498,
  499,   -1,   -1,   -1,  503,   -1,  505,  506,   -1,   -1,
   -1,   -1,  283,  432,  449,  450,  451,  452,  453,  454,
  455,  456,   -1,  458,   -1,  460,  461,  462,  463,   -1,
  449,  450,  451,  452,  453,  454,  455,  456,   -1,  458,
   -1,  460,  461,  462,  463,   -1,   -1,  258,  547,  260,
  261,   -1,  551,  552,   -1,   -1,   -1,   -1,  269,   -1,
   -1,   -1,   -1,  498,  499,   -1,   -1,   -1,  503,   -1,
  505,  506,  283,   -1,   -1,  432,   -1,   -1,   -1,  498,
  499,   -1,   -1,   -1,  503,   -1,  505,  506,   -1,   -1,
   -1,   -1,  449,  450,  451,  452,  453,  454,  455,  456,
   -1,  458,   -1,  460,  461,  462,  463,   -1,   -1,   -1,
   -1,   -1,  547,   -1,   -1,   -1,  551,  552,  405,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  547,   -1,
   -1,   -1,  551,  552,  405,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  432,  503,   -1,  505,  506,
   -1,  258,   -1,  260,  261,   -1,   -1,   -1,   -1,   -1,
   -1,  432,  449,  450,  451,  452,  453,  454,  455,  456,
   -1,  458,   -1,  460,  461,  462,  463,   -1,  449,  450,
  451,  452,  453,  454,  455,  456,   -1,  458,   -1,  460,
  461,  462,  463,  550,  405,  552,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  498,  499,   -1,   -1,   -1,  503,   -1,  505,  506,
   -1,  432,   -1,   -1,   -1,   -1,   -1,  498,  499,   -1,
   -1,   -1,  503,   -1,  505,  506,   -1,   -1,  449,  450,
  451,  452,  453,  454,  455,  456,   -1,  458,   -1,  460,
  461,  462,  463,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  547,   -1,   -1,   -1,  551,  552,   -1,   -1,   -1,   -1,
   -1,   -1,  379,   -1,   -1,   -1,  547,   -1,   -1,   -1,
  551,  552,   -1,   -1,   -1,  392,  393,  498,  499,  396,
   -1,  398,  503,   -1,  505,  506,   -1,   -1,  405,   -1,
  407,  408,  409,  410,  411,  412,  413,  414,   -1,  416,
  417,  418,   -1,  258,   -1,  260,   -1,   -1,   -1,   -1,
   -1,  266,  267,  268,  269,   -1,   -1,   -1,   -1,   -1,
  275,   -1,   -1,   -1,  279,  280,  547,  282,   -1,   -1,
  551,  552,   -1,   -1,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  279,   -1,   -1,  321,  322,  323,  324,
   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,   -1,  334,
  335,   -1,   -1,  338,   -1,  340,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  349,   -1,  351,  352,  353,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  258,  364,
  260,   -1,  367,   -1,  369,   -1,  266,  267,  268,  269,
   -1,   -1,   -1,   -1,   -1,  275,   -1,  544,   -1,  279,
  280,   -1,  282,  550,   -1,   -1,   -1,   -1,   -1,   -1,
  290,   -1,  292,  293,  294,  295,  296,   -1,  298,  299,
  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  321,  322,  323,  324,   -1,   -1,  327,   -1,   -1,
   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,  338,   -1,
  340,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  349,
   -1,  351,  352,  353,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,  369,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  446,  447,   -1,  449,  450,  451,  452,  453,  454,  455,
  456,  457,  458,   -1,   -1,  500,  501,  502,  464,  465,
  466,  467,  468,   -1,   -1,  471,  472,   -1,  474,  475,
   -1,   -1,   -1,   -1,   -1,   -1,  482,   -1,   -1,  485,
  486,  487,  488,  489,  490,  491,  492,  493,  494,  495,
  496,  497,  371,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  407,   -1,
   -1,   -1,   -1,  412,   -1,   -1,   -1,   -1,   -1,  418,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  500,  501,  502,  442,  443,   -1,   -1,  446,  447,  448,
  449,  450,  451,  452,  453,  454,  455,  456,  457,  458,
   -1,  460,  461,  462,  463,  464,  465,  466,  467,  468,
  469,  470,  471,  472,  473,  474,  475,  476,  477,  258,
   -1,  260,  481,   -1,   -1,   -1,   -1,   -1,  267,  268,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  498,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,  298,
  299,  300,  301,   -1,  303,  304,   -1,  306,  307,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  321,  322,  323,  324,   -1,   -1,  327,   -1,
   -1,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,  338,
   -1,  340,   -1,   -1,   -1,   -1,  258,   -1,  260,   -1,
  349,   -1,  351,  352,  353,  267,  268,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,
  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,
  292,  293,  294,  295,  296,   -1,  298,  299,  300,  301,
   -1,  303,  304,   -1,  306,  307,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  321,
  322,  323,  324,   -1,   -1,  327,   -1,   -1,   -1,   -1,
   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,   -1,
   -1,   -1,   -1,  258,   -1,  260,   -1,  349,   -1,  351,
  352,  353,  267,  268,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  364,   -1,   -1,  367,   -1,  369,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,  294,
  295,  296,   -1,  298,  299,  300,  301,   -1,  303,  304,
   -1,  306,  307,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  321,  322,  323,  324,
   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,   -1,  334,
  335,   -1,   -1,  338,   -1,  340,   -1,   -1,   -1,   -1,
  258,   -1,  260,   -1,  349,   -1,  351,  352,  353,  267,
  268,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  364,
   -1,   -1,  367,   -1,  369,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,  290,   -1,  292,  293,  294,  295,  296,   -1,
  298,  299,  300,  301,   -1,  303,  304,   -1,  306,  307,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  321,  322,  323,  324,   -1,   -1,  327,
   -1,   -1,   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,
  338,   -1,  340,   -1,   -1,   -1,   -1,  258,   -1,  260,
   -1,  349,   -1,  351,  352,  353,  267,  268,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  364,   -1,   -1,  367,
   -1,  369,   -1,   -1,   -1,   -1,   -1,   -1,   -1,  290,
   -1,  292,  293,  294,  295,  296,   -1,  298,  299,  300,
  301,   -1,  303,  304,   -1,  306,  307,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,   -1,
  321,  322,  323,  324,   -1,   -1,  327,   -1,   -1,   -1,
   -1,   -1,   -1,  334,  335,   -1,   -1,  338,   -1,  340,
   -1,   -1,   -1,   -1,  258,   -1,  260,   -1,  349,   -1,
  351,  352,  353,  267,  268,   -1,   -1,   -1,   -1,   -1,
   -1,   -1,   -1,  364,   -1,   -1,  367,   -1,  369,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,  290,   -1,  292,  293,
  294,  295,  296,   -1,  298,  299,  300,  301,   -1,  303,
  304,   -1,  306,  307,   -1,   -1,   -1,   -1,  268,   -1,
   -1,   -1,   -1,   -1,   -1,   -1,   -1,  321,  322,  323,
  324,   -1,   -1,  327,   -1,   -1,   -1,   -1,   -1,   -1,
  334,  335,   -1,   -1,  338,   -1,  340,   -1,   -1,   -1,
   -1,   -1,   -1,   -1,   -1,  349,   -1,  351,  352,  353,
  310,   -1,   -1,   -1,   -1,  315,   -1,   -1,  318,  319,
  364,  321,  322,  367,   -1,  369,   -1,   -1,  328,  329,
   -1,   -1,   -1,   -1,  334,  335,   -1,   -1,   -1,   -1,
   -1,  341,   -1,  343,  344,  345,   -1,   -1,   -1,   -1,
   -1,   -1,  352,  353,   -1,   -1,   -1,   -1,   -1,   -1,
   -1,  361,  362,   -1,   -1,   -1,   -1,   -1,  368,
  };

#line 3435 "/data/data/com.termux/files/pkg/ps/mono-4.9.0/mcs/ilasm/parser/ILParser.jay"


}

#line default
namespace yydebug {
        using System;
	 internal interface yyDebug {
		 void push (int state, Object value);
		 void lex (int state, int token, string name, Object value);
		 void shift (int from, int to, int errorFlag);
		 void pop (int state);
		 void discard (int state, int token, string name, Object value);
		 void reduce (int from, int to, int rule, string text, int len);
		 void shift (int from, int to);
		 void accept (Object value);
		 void error (string message);
		 void reject ();
	 }
	 
	 class yyDebugSimple : yyDebug {
		 void println (string s){
			 Console.Error.WriteLine (s);
		 }
		 
		 public void push (int state, Object value) {
			 println ("push\tstate "+state+"\tvalue "+value);
		 }
		 
		 public void lex (int state, int token, string name, Object value) {
			 println("lex\tstate "+state+"\treading "+name+"\tvalue "+value);
		 }
		 
		 public void shift (int from, int to, int errorFlag) {
			 switch (errorFlag) {
			 default:				// normally
				 println("shift\tfrom state "+from+" to "+to);
				 break;
			 case 0: case 1: case 2:		// in error recovery
				 println("shift\tfrom state "+from+" to "+to
					     +"\t"+errorFlag+" left to recover");
				 break;
			 case 3:				// normally
				 println("shift\tfrom state "+from+" to "+to+"\ton error");
				 break;
			 }
		 }
		 
		 public void pop (int state) {
			 println("pop\tstate "+state+"\ton error");
		 }
		 
		 public void discard (int state, int token, string name, Object value) {
			 println("discard\tstate "+state+"\ttoken "+name+"\tvalue "+value);
		 }
		 
		 public void reduce (int from, int to, int rule, string text, int len) {
			 println("reduce\tstate "+from+"\tuncover "+to
				     +"\trule ("+rule+") "+text);
		 }
		 
		 public void shift (int from, int to) {
			 println("goto\tfrom state "+from+" to "+to);
		 }
		 
		 public void accept (Object value) {
			 println("accept\tvalue "+value);
		 }
		 
		 public void error (string message) {
			 println("error\t"+message);
		 }
		 
		 public void reject () {
			 println("reject");
		 }
		 
	 }
}
// %token constants
 class Token {
  public const int EOF = 257;
  public const int ID = 258;
  public const int QSTRING = 259;
  public const int SQSTRING = 260;
  public const int COMP_NAME = 261;
  public const int INT32 = 262;
  public const int INT64 = 263;
  public const int FLOAT64 = 264;
  public const int HEXBYTE = 265;
  public const int DOT = 266;
  public const int OPEN_BRACE = 267;
  public const int CLOSE_BRACE = 268;
  public const int OPEN_BRACKET = 269;
  public const int CLOSE_BRACKET = 270;
  public const int OPEN_PARENS = 271;
  public const int CLOSE_PARENS = 272;
  public const int COMMA = 273;
  public const int COLON = 274;
  public const int DOUBLE_COLON = 275;
  public const int SEMICOLON = 277;
  public const int ASSIGN = 278;
  public const int STAR = 279;
  public const int AMPERSAND = 280;
  public const int PLUS = 281;
  public const int SLASH = 282;
  public const int BANG = 283;
  public const int ELLIPSIS = 284;
  public const int DASH = 286;
  public const int OPEN_ANGLE_BRACKET = 287;
  public const int CLOSE_ANGLE_BRACKET = 288;
  public const int UNKNOWN = 289;
  public const int INSTR_NONE = 290;
  public const int INSTR_VAR = 291;
  public const int INSTR_I = 292;
  public const int INSTR_I8 = 293;
  public const int INSTR_R = 294;
  public const int INSTR_BRTARGET = 295;
  public const int INSTR_METHOD = 296;
  public const int INSTR_NEWOBJ = 297;
  public const int INSTR_FIELD = 298;
  public const int INSTR_TYPE = 299;
  public const int INSTR_STRING = 300;
  public const int INSTR_SIG = 301;
  public const int INSTR_RVA = 302;
  public const int INSTR_TOK = 303;
  public const int INSTR_SWITCH = 304;
  public const int INSTR_PHI = 305;
  public const int INSTR_LOCAL = 306;
  public const int INSTR_PARAM = 307;
  public const int D_ADDON = 308;
  public const int D_ALGORITHM = 309;
  public const int D_ASSEMBLY = 310;
  public const int D_BACKING = 311;
  public const int D_BLOB = 312;
  public const int D_CAPABILITY = 313;
  public const int D_CCTOR = 314;
  public const int D_CLASS = 315;
  public const int D_COMTYPE = 316;
  public const int D_CONFIG = 317;
  public const int D_IMAGEBASE = 318;
  public const int D_CORFLAGS = 319;
  public const int D_CTOR = 320;
  public const int D_CUSTOM = 321;
  public const int D_DATA = 322;
  public const int D_EMITBYTE = 323;
  public const int D_ENTRYPOINT = 324;
  public const int D_EVENT = 325;
  public const int D_EXELOC = 326;
  public const int D_EXPORT = 327;
  public const int D_FIELD = 328;
  public const int D_FILE = 329;
  public const int D_FIRE = 330;
  public const int D_GET = 331;
  public const int D_HASH = 332;
  public const int D_IMPLICITCOM = 333;
  public const int D_LANGUAGE = 334;
  public const int D_LINE = 335;
  public const int D_XLINE = 336;
  public const int D_LOCALE = 337;
  public const int D_LOCALS = 338;
  public const int D_MANIFESTRES = 339;
  public const int D_MAXSTACK = 340;
  public const int D_METHOD = 341;
  public const int D_MIME = 342;
  public const int D_MODULE = 343;
  public const int D_MRESOURCE = 344;
  public const int D_NAMESPACE = 345;
  public const int D_ORIGINATOR = 346;
  public const int D_OS = 347;
  public const int D_OTHER = 348;
  public const int D_OVERRIDE = 349;
  public const int D_PACK = 350;
  public const int D_PARAM = 351;
  public const int D_PERMISSION = 352;
  public const int D_PERMISSIONSET = 353;
  public const int D_PROCESSOR = 354;
  public const int D_PROPERTY = 355;
  public const int D_PUBLICKEY = 356;
  public const int D_PUBLICKEYTOKEN = 357;
  public const int D_REMOVEON = 358;
  public const int D_SET = 359;
  public const int D_SIZE = 360;
  public const int D_STACKRESERVE = 361;
  public const int D_SUBSYSTEM = 362;
  public const int D_TITLE = 363;
  public const int D_TRY = 364;
  public const int D_VER = 365;
  public const int D_VTABLE = 366;
  public const int D_VTENTRY = 367;
  public const int D_VTFIXUP = 368;
  public const int D_ZEROINIT = 369;
  public const int K_AT = 370;
  public const int K_AS = 371;
  public const int K_IMPLICITCOM = 372;
  public const int K_IMPLICITRES = 373;
  public const int K_NOAPPDOMAIN = 374;
  public const int K_NOPROCESS = 375;
  public const int K_NOMACHINE = 376;
  public const int K_EXTERN = 377;
  public const int K_INSTANCE = 378;
  public const int K_EXPLICIT = 379;
  public const int K_DEFAULT = 380;
  public const int K_VARARG = 381;
  public const int K_UNMANAGED = 382;
  public const int K_CDECL = 383;
  public const int K_STDCALL = 384;
  public const int K_THISCALL = 385;
  public const int K_FASTCALL = 386;
  public const int K_MARSHAL = 387;
  public const int K_IN = 388;
  public const int K_OUT = 389;
  public const int K_OPT = 390;
  public const int K_STATIC = 391;
  public const int K_PUBLIC = 392;
  public const int K_PRIVATE = 393;
  public const int K_FAMILY = 394;
  public const int K_INITONLY = 395;
  public const int K_RTSPECIALNAME = 396;
  public const int K_STRICT = 397;
  public const int K_SPECIALNAME = 398;
  public const int K_ASSEMBLY = 399;
  public const int K_FAMANDASSEM = 400;
  public const int K_FAMORASSEM = 401;
  public const int K_PRIVATESCOPE = 402;
  public const int K_LITERAL = 403;
  public const int K_NOTSERIALIZED = 404;
  public const int K_VALUE = 405;
  public const int K_NOT_IN_GC_HEAP = 406;
  public const int K_INTERFACE = 407;
  public const int K_SEALED = 408;
  public const int K_ABSTRACT = 409;
  public const int K_AUTO = 410;
  public const int K_SEQUENTIAL = 411;
  public const int K_ANSI = 412;
  public const int K_UNICODE = 413;
  public const int K_AUTOCHAR = 414;
  public const int K_BESTFIT = 415;
  public const int K_IMPORT = 416;
  public const int K_SERIALIZABLE = 417;
  public const int K_NESTED = 418;
  public const int K_LATEINIT = 419;
  public const int K_EXTENDS = 420;
  public const int K_IMPLEMENTS = 421;
  public const int K_FINAL = 422;
  public const int K_VIRTUAL = 423;
  public const int K_HIDEBYSIG = 424;
  public const int K_NEWSLOT = 425;
  public const int K_UNMANAGEDEXP = 426;
  public const int K_PINVOKEIMPL = 427;
  public const int K_NOMANGLE = 428;
  public const int K_OLE = 429;
  public const int K_LASTERR = 430;
  public const int K_WINAPI = 431;
  public const int K_NATIVE = 432;
  public const int K_IL = 433;
  public const int K_CIL = 434;
  public const int K_OPTIL = 435;
  public const int K_MANAGED = 436;
  public const int K_FORWARDREF = 437;
  public const int K_RUNTIME = 438;
  public const int K_INTERNALCALL = 439;
  public const int K_SYNCHRONIZED = 440;
  public const int K_NOINLINING = 441;
  public const int K_CUSTOM = 442;
  public const int K_FIXED = 443;
  public const int K_SYSSTRING = 444;
  public const int K_ARRAY = 445;
  public const int K_VARIANT = 446;
  public const int K_CURRENCY = 447;
  public const int K_SYSCHAR = 448;
  public const int K_VOID = 449;
  public const int K_BOOL = 450;
  public const int K_INT8 = 451;
  public const int K_INT16 = 452;
  public const int K_INT32 = 453;
  public const int K_INT64 = 454;
  public const int K_FLOAT32 = 455;
  public const int K_FLOAT64 = 456;
  public const int K_ERROR = 457;
  public const int K_UNSIGNED = 458;
  public const int K_UINT = 459;
  public const int K_UINT8 = 460;
  public const int K_UINT16 = 461;
  public const int K_UINT32 = 462;
  public const int K_UINT64 = 463;
  public const int K_DECIMAL = 464;
  public const int K_DATE = 465;
  public const int K_BSTR = 466;
  public const int K_LPSTR = 467;
  public const int K_LPWSTR = 468;
  public const int K_LPTSTR = 469;
  public const int K_OBJECTREF = 470;
  public const int K_IUNKNOWN = 471;
  public const int K_IDISPATCH = 472;
  public const int K_STRUCT = 473;
  public const int K_SAFEARRAY = 474;
  public const int K_INT = 475;
  public const int K_BYVALSTR = 476;
  public const int K_TBSTR = 477;
  public const int K_LPVOID = 478;
  public const int K_ANY = 479;
  public const int K_FLOAT = 480;
  public const int K_LPSTRUCT = 481;
  public const int K_NULL = 482;
  public const int K_PTR = 483;
  public const int K_VECTOR = 484;
  public const int K_HRESULT = 485;
  public const int K_CARRAY = 486;
  public const int K_USERDEFINED = 487;
  public const int K_RECORD = 488;
  public const int K_FILETIME = 489;
  public const int K_BLOB = 490;
  public const int K_STREAM = 491;
  public const int K_STORAGE = 492;
  public const int K_STREAMED_OBJECT = 493;
  public const int K_STORED_OBJECT = 494;
  public const int K_BLOB_OBJECT = 495;
  public const int K_CF = 496;
  public const int K_CLSID = 497;
  public const int K_METHOD = 498;
  public const int K_CLASS = 499;
  public const int K_PINNED = 500;
  public const int K_MODREQ = 501;
  public const int K_MODOPT = 502;
  public const int K_TYPEDREF = 503;
  public const int K_TYPE = 504;
  public const int K_WCHAR = 505;
  public const int K_CHAR = 506;
  public const int K_FROMUNMANAGED = 507;
  public const int K_CALLMOSTDERIVED = 508;
  public const int K_BYTEARRAY = 509;
  public const int K_WITH = 510;
  public const int K_INIT = 511;
  public const int K_TO = 512;
  public const int K_CATCH = 513;
  public const int K_FILTER = 514;
  public const int K_FINALLY = 515;
  public const int K_FAULT = 516;
  public const int K_HANDLER = 517;
  public const int K_TLS = 518;
  public const int K_FIELD = 519;
  public const int K_PROPERTY = 520;
  public const int K_REQUEST = 521;
  public const int K_DEMAND = 522;
  public const int K_ASSERT = 523;
  public const int K_DENY = 524;
  public const int K_PERMITONLY = 525;
  public const int K_LINKCHECK = 526;
  public const int K_INHERITCHECK = 527;
  public const int K_REQMIN = 528;
  public const int K_REQOPT = 529;
  public const int K_REQREFUSE = 530;
  public const int K_PREJITGRANT = 531;
  public const int K_PREJITDENY = 532;
  public const int K_NONCASDEMAND = 533;
  public const int K_NONCASLINKDEMAND = 534;
  public const int K_NONCASINHERITANCE = 535;
  public const int K_READONLY = 536;
  public const int K_NOMETADATA = 537;
  public const int K_ALGORITHM = 538;
  public const int K_FULLORIGIN = 539;
  public const int K_ENABLEJITTRACKING = 540;
  public const int K_DISABLEJITOPTIMIZER = 541;
  public const int K_RETARGETABLE = 542;
  public const int K_PRESERVESIG = 543;
  public const int K_BEFOREFIELDINIT = 544;
  public const int K_ALIGNMENT = 545;
  public const int K_NULLREF = 546;
  public const int K_VALUETYPE = 547;
  public const int K_COMPILERCONTROLLED = 548;
  public const int K_REQSECOBJ = 549;
  public const int K_ENUM = 550;
  public const int K_OBJECT = 551;
  public const int K_STRING = 552;
  public const int K_TRUE = 553;
  public const int K_FALSE = 554;
  public const int K_IS = 555;
  public const int K_ON = 556;
  public const int K_OFF = 557;
  public const int K_FORWARDER = 558;
  public const int K_CHARMAPERROR = 559;
  public const int K_LEGACY = 560;
  public const int K_LIBRARY = 561;
  public const int yyErrorCode = 256;
 }
 namespace yyParser {
  using System;
  /** thrown for irrecoverable syntax errors and stack overflow.
    */
  internal class yyException : System.Exception {
    public yyException (string message) : base (message) {
    }
  }
  internal class yyUnexpectedEof : yyException {
    public yyUnexpectedEof (string message) : base (message) {
    }
    public yyUnexpectedEof () : base ("") {
    }
  }

  /** must be implemented by a scanner object to supply input to the parser.
    */
  internal interface yyInput {
    /** move on to next token.
        @return false if positioned beyond tokens.
        @throws IOException on input error.
      */
    bool advance (); // throws java.io.IOException;
    /** classifies current token.
        Should not be called if advance() returned false.
        @return current %token or single character.
      */
    int token ();
    /** associated with current token.
        Should not be called if advance() returned false.
        @return value for token().
      */
    Object value ();
  }
 }
} // close outermost namespace, that MUST HAVE BEEN opened in the prolog
