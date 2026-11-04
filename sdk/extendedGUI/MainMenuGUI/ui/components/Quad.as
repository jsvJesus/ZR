package ui.components
{
   import com.dvalimona.components.Component;
   import flash.display.*;
   
   public class Quad extends Component
   {
      private var holder:Sprite;
      
      private var _thickness:int = 2;
      
      protected var color:uint;
      
      public function Quad(parent:DisplayObjectContainer = null, color:uint = 16777215, alpha:Number = 0.5)
      {
         this.color = color;
         this.alpha = alpha;
         super(parent);
      }
      
      public function get thickness() : int
      {
         return this._thickness;
      }
      
      public function set thickness(value:int) : void
      {
         this._thickness = value;
         invalidate();
      }
      
      override protected function addChildren() : void
      {
         this.holder = new Sprite();
         this.holder.alpha = alpha;
         this.addChild(this.holder);
      }
      
      override public function draw() : void
      {
         if(this.holder)
         {
            this.holder.graphics.clear();
            this.holder.graphics.beginFill(this.color,1);
            this.holder.graphics.drawRoundRect(0,height / 2 - this.thickness / 2,width,this.thickness,2,2);
            this.holder.graphics.endFill();
         }
      }
   }
}

