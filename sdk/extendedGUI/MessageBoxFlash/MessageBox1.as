package
{
   import flash.display.MovieClip;
   import flash.events.MouseEvent;
   import flash.text.TextField;
   
   public dynamic class MessageBox1 extends MovieClip
   {
      public var MsgText:TextField;
      
      public var NoBtn:SOButton;
      
      public var Title:TextField;
      
      public var YesBtn:SOButton;
      
      public var background:MovieClip;
      
      public function MessageBox1()
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

