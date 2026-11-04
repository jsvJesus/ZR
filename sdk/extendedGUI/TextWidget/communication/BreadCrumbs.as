package communication
{
   import events.*;
   import flash.events.*;
   
   public class BreadCrumbs extends EventDispatcher
   {
      private static var _path:Vector.<BreadCrumb>;
      
      private static var _self:BreadCrumbs;
      
      public static const DIV:String = "/ ";
      
      _path = new Vector.<BreadCrumb>();
      
      public function BreadCrumbs(param1:IEventDispatcher = null)
      {
         super(param1);
      }
      
      public static function get Path() : Vector.<BreadCrumb>
      {
         return _path;
      }
      
      public static function set Path(param1:Vector.<BreadCrumb>) : void
      {
         _path = param1;
         self.dispatchEvent(new Event(Event.CHANGE));
      }
      
      public static function Add(param1:BreadCrumb) : void
      {
         if(_path.indexOf(param1) < 0)
         {
            _path.push(param1);
            BreadCrumbs.getItemLabels();
            self.dispatchEvent(new BreadCrumbEvent(BreadCrumbEvent.ADDED,param1));
         }
      }
      
      public static function Remove(param1:BreadCrumb) : void
      {
         if(_path.indexOf(param1) >= 0)
         {
            _path.splice(_path.indexOf(param1),1);
            self.dispatchEvent(new BreadCrumbEvent(BreadCrumbEvent.REMOVED,param1));
         }
         BreadCrumbs.getItemLabels();
      }
      
      public static function getItemLabels() : String
      {
         var _loc1_:* = "";
         var _loc2_:* = 0;
         while(_loc2_ < _path.length)
         {
            _loc1_ += _path[_loc2_].screen.label + "; ";
            _loc2_++;
         }
         return _loc1_;
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

