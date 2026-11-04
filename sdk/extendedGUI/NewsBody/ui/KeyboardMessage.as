package ui
{
   import com.dvalimona.components.Component;
   import com.dvalimona.components.Label;
   import com.dvalimona.components.LabelShadowed;
   import com.greensock.TweenMax;
   import com.greensock.easing.Expo;
   import flash.events.Event;
   
   public class KeyboardMessage extends Component
   {
      private var instructionLabel:LabelShadowed;
      
      private var cancelLabel:LabelShadowed;
      
      private var actionLabel:LabelShadowed;
      
      public var targetButton:*;
      
      public var labelText:String = "hello";
      
      private var cleared:Boolean = false;
      
      public function KeyboardMessage(labelText:String, targetButton:*)
      {
         this.labelText = labelText;
         this.targetButton = targetButton;
         this.addChildren();
         super();
      }
      
      override public function set width(w:Number) : void
      {
         _width = w;
         this.draw();
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      override public function set height(h:Number) : void
      {
         _height = h;
         this.draw();
         dispatchEvent(new Event(Event.RESIZE));
      }
      
      override protected function init() : void
      {
         setSize(100,100);
      }
      
      override protected function addChildren() : void
      {
         this.actionLabel = new LabelShadowed();
         this.actionLabel.size = 50;
         this.actionLabel.text = this.labelText;
         this.actionLabel.autoSize = true;
         this.actionLabel.align = Label.CENTER;
         this.actionLabel.debug = false;
         this.addChild(this.actionLabel);
         this.instructionLabel = new LabelShadowed();
         this.instructionLabel.size = 16;
         this.instructionLabel.color = 10526608;
         this.instructionLabel.$ = "extendedGUI.Settings.keybindsInstruction1";
         this.instructionLabel.autoSize = false;
         this.instructionLabel.align = Label.LEFT;
         this.instructionLabel.debug = false;
         this.addChild(this.instructionLabel);
         this.cancelLabel = new LabelShadowed();
         this.cancelLabel.size = 16;
         this.cancelLabel.color = 10526608;
         this.cancelLabel.$ = "extendedGUI.Settings.keybindsInstruction2";
         this.cancelLabel.autoSize = false;
         this.cancelLabel.align = Label.RIGHT;
         this.cancelLabel.debug = false;
         this.addChild(this.cancelLabel);
         this.actionLabel.alpha = 0;
         this.instructionLabel.alpha = 0;
         this.cancelLabel.alpha = 0;
         TweenMax.to(this.actionLabel,0.2,{
            "alpha":1,
            "delay":0.1,
            "ease":Expo.easeOut,
            "onComplete":null
         });
         TweenMax.to(this.instructionLabel,0.2,{
            "alpha":1,
            "delay":0.2,
            "ease":Expo.easeOut,
            "onComplete":null
         });
         TweenMax.to(this.cancelLabel,0.2,{
            "alpha":1,
            "delay":0.3,
            "ease":Expo.easeOut,
            "onComplete":null
         });
      }
      
      public function clear() : void
      {
         this.cleared = true;
         this.actionLabel.alpha = 0;
         this.instructionLabel.alpha = 0;
         this.cancelLabel.alpha = 0;
      }
      
      override public function draw() : void
      {
         var minWidth:uint = Math.max(this.actionLabel.width,200);
         this.actionLabel.x = (width - minWidth) / 2;
         this.actionLabel.y = (height - this.actionLabel.height) / 2;
         this.actionLabel.width = minWidth;
         this.instructionLabel.x = this.actionLabel.x;
         this.instructionLabel.width = minWidth;
         this.instructionLabel.y = this.actionLabel.y - this.instructionLabel.height - 0;
         this.cancelLabel.x = this.instructionLabel.x;
         this.cancelLabel.y = this.actionLabel.y + this.actionLabel.height + 0;
         this.cancelLabel.width = minWidth;
         this.graphics.clear();
         this.graphics.beginFill(0,0.96);
         this.graphics.drawRect(0,0,width,height);
         if(!this.cleared)
         {
            this.graphics.beginFill(16777215,0.1);
            this.graphics.drawRect(this.instructionLabel.x - 10,this.instructionLabel.y - 10,minWidth + 20,this.cancelLabel.y + this.cancelLabel.height - this.instructionLabel.y + 20);
            this.graphics.beginFill(16777215,0.2);
            this.graphics.drawRect(this.instructionLabel.x - 10,this.actionLabel.y - 0,minWidth + 20,this.cancelLabel.y - this.actionLabel.y);
         }
         this.graphics.endFill();
      }
   }
}

