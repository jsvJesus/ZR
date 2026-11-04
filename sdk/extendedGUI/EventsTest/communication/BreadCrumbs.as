package communication
{
   import events.BreadCrumbEvent;
   import flash.events.Event;
   import flash.events.EventDispatcher;
   import flash.events.IEventDispatcher;
   
   public class BreadCrumbs extends EventDispatcher
   {
      private static var _self:BreadCrumbs;
      
      public static const DIV:String = "/ ";
      
      private static var _path:Vector.<BreadCrumb> = new Vector.<BreadCrumb>();
      
      public function BreadCrumbs(target:IEventDispatcher = null)
      {
         super(target);
      }
      
      public static function get Path() : Vector.<BreadCrumb>
      {
         return _path;
      }
      
      public static function set Path(value:Vector.<BreadCrumb>) : void
      {
         _path = value;
         self.dispatchEvent(new Event(Event.CHANGE));
      }
      
      public static function Add(value:BreadCrumb) : void
      {
         if(_path.indexOf(value) < 0)
         {
            _path.push(value);
            BreadCrumbs.getItemLabels();
            self.dispatchEvent(new BreadCrumbEvent(BreadCrumbEvent.ADDED,value));
         }
      }
      
      public static function Remove(value:BreadCrumb) : void
      {
         if(_path.indexOf(value) >= 0)
         {
            _path.splice(_path.indexOf(value),1);
            self.dispatchEvent(new BreadCrumbEvent(BreadCrumbEvent.REMOVED,value));
         }
         BreadCrumbs.getItemLabels();
      }
      
      public static function getItemLabels() : String
      {
         var result:String = "";
         for(var i:uint = 0; i < _path.length; i++)
         {
            result += _path[i].screen.label + "; ";
         }
         return result;
      }
      
      public static function get self() : BreadCrumbs
      {
         if(!_self)
         {
            _self = new BreadCrumbs();
         }
         return _self;
      }
   }
}

