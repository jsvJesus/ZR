package events
{
   import communication.BreadCrumb;
   import flash.events.Event;
   
   public class BreadCrumbEvent extends Event
   {
      public static const ADDED:String = "crumb_added";
      
      public static const REMOVED:String = "crumb_removed";
      
      public static const CHANGED:String = "crumbs_changed";
      
      private var _data:BreadCrumb;
      
      public function BreadCrumbEvent(type:String, bc:BreadCrumb, bubbles:Boolean = false, cancelable:Boolean = false)
      {
         this._data = bc;
         super(type,bubbles,cancelable);
      }
      
      public function get data() : BreadCrumb
      {
         return this._data;
      }
   }
}

