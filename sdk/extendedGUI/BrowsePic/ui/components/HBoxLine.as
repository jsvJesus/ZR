package ui.components
{
   import com.dvalimona.components.Component;
   import com.dvalimona.components.HBox;
   import flash.display.DisplayObjectContainer;
   
   public class HBoxLine extends Component
   {
      public var left:HBox;
      
      public var right:HBox;
      
      public function HBoxLine(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
         this.left = new HBox(this);
         this.left.spacing = 1;
         this.left.horizontalAlign = HBox.LEFT;
         this.right = new HBox(this);
         this.right.spacing = 1;
         this.right.horizontalAlign = HBox.RIGHT;
      }
      
      override public function draw() : void
      {
         super.draw();
         trace("HBoxLine.draw",_width,_height);
         this.left.fixedWidth = _width;
         this.left.fixedHeight = _height;
         this.right.fixedWidth = _width;
         this.right.fixedHeight = _height;
      }
   }
}

