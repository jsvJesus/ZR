package
{
   import com.MessageBox;
   import flash.events.MouseEvent;
   
   public dynamic class MessageBox extends com.MessageBox
   {
      public function MessageBox()
      {
         super();
      }
      
      public function onMessageStartDrag(e:MouseEvent) : *
      {
         this.startDrag();
         stage.addEventListener(MouseEvent.MOUSE_UP,this.onMessageStopDrag);
      }
      
      public function onMessageStopDrag(e:MouseEvent) : *
      {
         this.stopDrag();
         stage.removeEventListener(MouseEvent.MOUSE_UP,this.onMessageStopDrag);
      }
   }
}

