package events
{
   import communication.*;
   import flash.events.*;
   
   public class BreadCrumbEvent extends Event
   {
      public static const ADDED:String = "crumb_added";
      
      public static const REMOVED:String = "crumb_removed";
      
      public static const CHANGED:String = "crumbs_changed";
      
      private var _data:BreadCrumb;
      
      public function BreadCrumbEvent(param1:String, param2:BreadCrumb, param3:Boolean = false, param4:Boolean = false)
      {
         this._data = param2;
         super(param1,param3,param4);
      }
      
      public function get data() : BreadCrumb
      {
         return this._data;
      }
   }
}

