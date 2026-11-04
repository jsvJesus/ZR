package com
{
   public class Commands
   {
      public static var _commands:Object = new Object();
      
      public function Commands()
      {
         super();
      }
      
      public static function parseCommands(Dict:Array) : *
      {
         var i:* = undefined;
         var j:String = null;
         for(i in Dict)
         {
            j = Dict[i].charAt(0).toUpperCase();
            if(_commands[j] == null)
            {
               _commands[j] = new Array();
            }
            _commands[j].push(Dict[i]);
         }
      }
      
      protected static function getCommandForCommandList(command:String, list:Array) : Array
      {
         var i:* = undefined;
         var resul:Array = new Array();
         for(i in list)
         {
            if(StringParse.findIn(command,list[i]))
            {
               resul.push(list[i]);
            }
         }
         if(resul.length > 0)
         {
            return resul;
         }
         return null;
      }
      
      protected static function findListCommand(command:String) : Array
      {
         var ch:String = null;
         if(command != "null" && command != "")
         {
            ch = command.charAt(0).toUpperCase();
            return getCommandForCommandList(command,_commands[ch]);
         }
         return null;
      }
      
      protected static function findCom(txt:String) : Array
      {
         var command:String = "";
         var comand:Boolean = false;
         for(var i:* = 0; i < txt.length; i++)
         {
            if(txt.charAt(i) == " ")
            {
               if(comand)
               {
                  return new Array(i - 1,command);
               }
            }
            else if(comand)
            {
               command += txt.charAt(i);
            }
            else
            {
               if(txt.charAt(i) != "/")
               {
                  return new Array(-1,"null");
               }
               comand = true;
            }
         }
         if(comand)
         {
            return new Array(txt.length - 1,command);
         }
         return new Array(-1,"null");
      }
      
      public static function getCommands(text:String) : Array
      {
         return findListCommand(findCom(text)[1]);
      }
   }
}

