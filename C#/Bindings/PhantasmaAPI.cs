using System;
using System.Net;
using System.Collections;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using UnityEngine;
using UnityEngine.Networking;
using Phantasma.Cryptography;

namespace Phantasma.SDK
{
	public static class APIUtils
    {
        public static long GetInt64(this DataNode node, string name)
        {
            return node.GetLong(name);
        }

        public static bool GetBoolean(this DataNode node, string name)
        {
            return node.GetBool(name);
        }
    }

    internal class JSONRPC_Client
    {
        private WebClient client;

        internal JSONRPC_Client()
        {
            client = new WebClient() { Encoding = System.Text.Encoding.UTF8 }; 
        }

        internal IEnumerator SendRequest(string url, string method, Action<DataNode> callback, params object[] parameters)
        {
            string contents;

            DataNode paramData;

            if (parameters!=null && parameters.Length > 0)
            {
                paramData = DataNode.CreateArray("params");
                foreach (var obj in parameters)
                {
                    paramData.AddField(null, obj);
                }
            }
            else
            {
                paramData = null;
            }

            var jsonRpcData = DataNode.CreateObject(null);
            jsonRpcData.AddField("jsonrpc", "2.0");
            jsonRpcData.AddField("method", method);
            jsonRpcData.AddField("id", "1");

            if (paramData != null)
            {
                jsonRpcData.AddNode(paramData);
            }

            UnityWebRequest www;
            string json;

            try
            {
                //client.Headers.Add("Content-Type", "application/json-rpc");
				json = JSONWriter.WriteToString(jsonRpcData);
				//contents = client.UploadString(url, json);
            }
            catch (Exception e)
            {
                throw e;
            }
            
            www = UnityWebRequest.Post(url, json);
            yield return www.SendWebRequest();
            
            if (www.isNetworkError || www.isHttpError)
            {
                Debug.Log(www.error);
				throw new Exception(www.error);
            }
            else
            {
                Debug.Log(www.downloadHandler.text);
				var root = JSONReader.ReadFromString(www.downloadHandler.text);
				
				if (root == null)
				{
					throw new Exception("failed to parse JSON");
				}
				else 
				if (root.HasNode("error")) {
					var errorDesc = root.GetString("error");
					throw new Exception(errorDesc);
				}
				else
				if (root.HasNode("result"))
				{
					var result = root["result"];
					callback(result);
				}
				else {					
					throw new Exception("malformed response");
				}				
            }

			yield break;
        }		
   }
   
   {{#each types}}
	public struct {{#fix-type Key}} 
	{
{{#each Value}}		public {{#fix-type FieldType.Name}}{{#if FieldType.IsArray}}[]{{/if}} {{Name}};
{{/each}}	   
		public static {{#fix-type Key}} FromNode(DataNode node) 
		{
			{{#fix-type Key}} result;
{{#each Value}}			{{#if FieldType.IsArray}}
			var {{Name}}_array = node.GetNode("{{#camel-case Name}}");
			if ({{Name}}_array != null) {
				result.{{Name}} = new {{#fix-type FieldType.Name}}[{{Name}}_array.ChildCount];
				for (int i=0; i < {{Name}}_array.ChildCount; i++) {
					{{#if FieldType.Name contains 'Result'}}
					result.{{Name}}[i] = {{#fix-type FieldType.Name}}.FromNode({{Name}}_array.GetNodeByIndex(i));
					{{#else}}
					result.{{Name}}[i] = {{Name}}_array.GetNodeByIndex(i).As{{#fix-type FieldType.Name}}();{{/if}}
				}
			}
			else {
				result.{{Name}} = new {{#fix-type FieldType.Name}}[0];
			}
			{{#else}}			
			result.{{Name}} = node.Get{{FieldType.Name}}("{{#camel-case Name}}");{{/if}}{{/each}}

			return result;			
		}
	}
	{{/each}}
   
   public class API {	   
		public readonly	string Host;
		private static JSONRPC_Client _client;
	   
		public API(string host) 
		{
			this.Host = host;
			_client = new JSONRPC_Client();
		}
	   
		{{#each methods}}
		//{{Info.Description}}
		public IEnumerator {{Info.Name}}({{#each Info.Parameters}}{{#fix-type Key.Name}} {{Value}}, {{/each}}Action<{{#fix-type Info.ReturnType.Name}}{{#if Info.ReturnType.IsArray}}[]{{/if}}> callback)  
		{	   
			yield return _client.SendRequest(Host, "{{#camel-case Info.Name}}", (node) => { 
{{#parse-lines false}}
{{#if Info.ReturnType.IsPrimitive}}
			var result = {{#fix-type Info.ReturnType.Name}}.Parse(node.Value);
{{#else}}
{{#if Info.ReturnType.Name=='String'}}
			var result = node.Value;
{{#else}}
{{#if Info.ReturnType.IsArray}}
			var result = new {{#fix-type Info.ReturnType.Name}}[node.ChildCount];{{#new-line}}
			for (int i=0; i<result.Length; i++) { {{#new-line}}
				var child = node.GetNodeByIndex(i);{{#new-line}}
				result[i] = {{#fix-type Info.ReturnType.Name}}.FromNode(child);{{#new-line}}
			}
{{#else}}
			var result = {{#fix-type Info.ReturnType.Name}}.FromNode(node);
{{/if}}
{{/if}}
{{/if}}{{#parse-lines true}}
				callback(result);
			} {{#each Info.Parameters}}, {{Value}}{{/each}});		   
		}
		
		{{/each}}
	}
}