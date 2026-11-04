package com.brokenfunction.json
{
   public var encodeJson:Function = initDecodeJson();
}

import flash.utils.ByteArray;
import flash.utils.IDataOutput;

function initDecodeJson():Function
{
   var parseArray:Function;
   var parseString:Function;
   var result:IDataOutput = null;
   var i:int = 0;
   var j:int = 0;
   var strLen:int = 0;
   var str:String = null;
   var char:int = 0;
   var tempBytes:ByteArray = null;
   var blockNonFiniteNumbers:Boolean = false;
   var charConvert:Array = null;
   var parse:Object = null;
   tempBytes = new ByteArray();
   charConvert = new Array(256);
   for(j = 0; j < 10; j++)
   {
      charConvert[j] = j + 48 | 0x30303000;
   }
   while(j < 16)
   {
      charConvert[j] = j + 55 | 0x30303000;
      j++;
   }
   while(j < 26)
   {
      charConvert[j] = j + 32 | 0x30303100;
      j++;
   }
   while(j < 32)
   {
      charConvert[j] = j + 39 | 0x30303100;
      j++;
   }
   while(j < 256)
   {
      charConvert[j] = j;
      j++;
   }
   charConvert[10] = 23662;
   charConvert[13] = 23666;
   charConvert[9] = 23668;
   charConvert[8] = 23650;
   charConvert[12] = 23654;
   charConvert[8] = 23650;
   charConvert[34] = 23586;
   charConvert[92] = 23644;
   charConvert[127] = 808466246;
   parseArray = function(data:Array):void
   {
      result.writeByte(91);
      var k:int = 0;
      var len:int = data.length - 1;
      if(len >= 0)
      {
         while(k < len)
         {
            parse[typeof data[k]](data[k]);
            result.writeByte(44);
            k++;
         }
         parse[typeof data[k]](data[k]);
      }
      result.writeByte(93);
   };
   parseString = function(data:String):void
   {
      result.writeByte(34);
      tempBytes.position = 0;
      tempBytes.length = 0;
      tempBytes.writeUTFBytes(data);
      i = 0;
      j = 0;
      strLen = tempBytes.length;
      while(j < strLen)
      {
         char = charConvert[tempBytes[j++]];
         if(char > 256)
         {
            if(j - 1 > i)
            {
               result.writeBytes(tempBytes,i,j - 1 - i);
            }
            if(char > 65536)
            {
               result.writeShort(23669);
               result.writeUnsignedInt(char);
            }
            else
            {
               result.writeShort(char);
            }
            i = j;
         }
      }
      if(strLen > i)
      {
         result.writeBytes(tempBytes,i,strLen - i);
      }
      result.writeByte(34);
   };
   parse = {
      "object":function(data:Object):void
      {
         var first:* = undefined;
         if(Boolean(data))
         {
            if(data is Array)
            {
               parseArray(data);
            }
            else
            {
               result.writeByte(123);
               first = true;
               for(str in data)
               {
                  if(first)
                  {
                     first = false;
                  }
                  else
                  {
                     result.writeByte(44);
                  }
                  parseString(str);
                  result.writeByte(58);
                  parse[typeof data[str]](data[str]);
               }
               result.writeByte(125);
            }
         }
         else
         {
            result.writeUnsignedInt(1853189228);
         }
      },
      "string":parseString,
      "number":function(data:Number):void
      {
         if(blockNonFiniteNumbers && !isFinite(data))
         {
            throw new Error("Number " + data + " is not encodable");
         }
         result.writeUTFBytes(String(data));
      },
      "boolean":function(data:Boolean):void
      {
         if(data)
         {
            result.writeUnsignedInt(1953658213);
         }
         else
         {
            result.writeByte(102);
            result.writeUnsignedInt(1634497381);
         }
      },
      "xml":function(data:Object):void
      {
         throw new Error("unserializable XML object encountered");
      },
      "undefined":function(data:Boolean):void
      {
         result.writeUnsignedInt(1853189228);
      }
   };
   return function(input:Object, writeTo:IDataOutput = null, strictNumberSupport:Boolean = false, allowNativeJson:Boolean = false):String
   {
      var byteOutput:* = undefined;
      blockNonFiniteNumbers = strictNumberSupport;
      try
      {
         if(Boolean(writeTo))
         {
            result = writeTo;
            result.endian = "bigEndian";
            parse[typeof input](input);
            byteOutput.position = 0;
            return byteOutput.readUTFBytes(byteOutput.length);
         }
         switch(typeof input)
         {
            case "xml":
               throw new Error("unserializable XML object encountered");
            case "object":
            case "string":
               result = byteOutput = new ByteArray();
               result.endian = "bigEndian";
               parse[typeof input](input);
               byteOutput.position = 0;
               return byteOutput.readUTFBytes(byteOutput.length);
            case "number":
               if(blockNonFiniteNumbers && !isFinite(input as Number))
               {
                  throw new Error("Number " + input + " is not encodable");
               }
               return String(input);
               break;
            case "boolean":
               return Boolean(input) ? "true" : "false";
            case "undefined":
               return "null";
            default:
               throw new Error("Unexpected type \"" + typeof input + "\" encountered");
         }
      }
      catch(e:TypeError)
      {
         throw new Error("Unexpected type encountered");
      }
   };
}
