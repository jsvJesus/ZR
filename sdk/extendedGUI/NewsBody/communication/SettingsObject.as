package communication
{
   import logging.Logger;
   
   public class SettingsObject
   {
      public var path:Array;
      
      public function SettingsObject(initPath:Array)
      {
         super();
         this.path = initPath;
      }
      
      public static function Explore(source:Object, way:Array) : Object
      {
         var result:Object = null;
         var target:Object = source;
         for(var i:int = 0; i < way.length; i++)
         {
            if(Boolean(target[way[i]]))
            {
               if(target.hasOwnProperty(way[i]))
               {
                  result = target[way[i]];
                  target = result;
               }
            }
         }
         return result;
      }
      
      public static function makeApiObjectFromPathArray(p:Array, v:*) : Object
      {
         var obj:Object = {};
         Logger.LogToChannel(Logger.DEBUG,"makeObjectFromPathArray",v,Number(v));
         switch(p.length)
         {
            case 1:
               obj[p[0]] = v;
               break;
            case 2:
               obj[p[0]] = {};
               obj[p[0]][p[1]] = v;
               break;
            case 3:
               obj[p[0]] = {};
               obj[p[0]][p[1]] = {};
               obj[p[0]][p[1]][p[2]] = v;
               break;
            case 4:
               obj[p[0]] = {};
               obj[p[0]][p[1]] = {};
               obj[p[0]][p[1]][p[2]] = {};
               obj[p[0]][p[1]][p[2]][p[3]] = v;
               break;
            case 5:
               obj[p[0]] = {};
               obj[p[0]][p[1]] = {};
               obj[p[0]][p[1]][p[2]] = {};
               obj[p[0]][p[1]][p[2]][p[3]] = {};
               obj[p[0]][p[1]][p[2]][p[3]][p[4]] = v;
         }
         Logger.LogToChannel(Logger.DEBUG,"makeObjectFromPathArray",obj);
         return obj;
      }
      
      public static function merge(p:Array) : Object
      {
         var obj:Object = null;
         var prop:String = null;
         var merged:Object = {};
         for each(obj in p)
         {
            for(prop in obj)
            {
               if(merged[prop] != null)
               {
                  merged[prop] = merge([merged[prop],obj[prop]]);
               }
               else
               {
                  merged[prop] = obj[prop];
               }
            }
         }
         return merged;
      }
      
      public function get data() : Object
      {
         return Explore(Settings.Data,this.path);
      }
      
      public function get range() : Object
      {
         return Explore(Settings.Ranges,this.path);
      }
      
      public function get defaultData() : Object
      {
         return Explore(Settings.Default,this.path);
      }
   }
}

