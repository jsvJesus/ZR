package com
{
   public class ParseChannels
   {
      public function ParseChannels()
      {
         super();
      }
      
      public function parse(a:Array, defaultChannals:Array, sounds:Array) : Array
      {
         var Obj:Object = null;
         var j:* = undefined;
         var arr:Array = new Array();
         for(var i:* = 0; i < defaultChannals.length; i++)
         {
            Obj = new Object();
            Obj.id = defaultChannals[i].id;
            Obj.label = defaultChannals[i].label;
            Obj.selected = defaultChannals[i].selected;
            Obj.com = defaultChannals[i].com;
            Obj.short_name = defaultChannals[i].short_name;
            for(j = 0; j < a.length; j++)
            {
               if(defaultChannals[i].id == a[j])
               {
                  Obj.selected = true;
               }
            }
            for(j = 0; j < sounds.length; j++)
            {
               if(defaultChannals[i].id == sounds[j])
               {
                  Obj.sound = true;
               }
            }
            arr.push(Obj);
         }
         return arr;
      }
      
      public function unparse(settings:Array) : Object
      {
         var result:Object = new Object();
         result.sounds = new Array();
         result.channels = new Array();
         for(var i:* = 0; i < settings.length; i++)
         {
            if(settings[i].selected == true)
            {
               if(settings[i].id != -99)
               {
                  result.channels.push(settings[i].id);
               }
            }
            if(settings[i].sound == true)
            {
               result.sounds.push(settings[i].id);
            }
         }
         return result;
      }
   }
}

