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
      
      public function BreadCrumbEvent(arg1:String, arg2:BreadCrumb, arg3:Boolean = false, arg4:Boolean = false)
      {
         this._data = arg2;
         super(arg1,arg3,arg4);
      }
      
      public function get data() : BreadCrumb
      {
         return this._data;
      }
   }
}

