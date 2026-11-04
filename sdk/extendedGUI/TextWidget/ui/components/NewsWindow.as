package ui.components
{
   import com.dvalimona.components.*;
   import communication.*;
   import flash.events.*;
   
   public class NewsWindow extends DialogWindow
   {
      protected var rememberShowBox:HBox;
      
      protected var rememberShowBoxHeight:uint = 50;
      
      protected var rememberShowLabel:Label;
      
      protected var rememberShowCheck:CheckBox;
      
      public function NewsWindow(param1:Array, param2:Boolean = false)
      {
         this.sideMargin = 30;
         this.topMargin = 20;
         this.bottomMargin = 0;
         super(param1,param2);
      }
      
      override protected function bodyDeals() : void
      {
         super.bodyDeals();
         this.rememberShowBox = new HBox();
         this.rememberShowBox.alignment = HBox.MIDDLE;
         this.rememberShowBox.fixedHeight = this.rememberShowBoxHeight;
         this.rememberShowBox.horizontalAlign = HBox.RIGHT;
         this.rememberShowBox.debug = false;
         this.rememberShowCheck = new CheckBox();
         this.rememberShowCheck.debug = false;
         this.rememberShowCheck.focusMarginX = 6;
         this.rememberShowCheck.focusMarginY = 6;
         this.rememberShowCheck.tabEnabled = true;
         this.rememberShowCheck.selected = News.doNotShowNewsWindowAnymore;
         this.rememberShowCheck.addEventListener(Event.CHANGE,this.onShowCheckChange);
         this.rememberShowBox.addChild(this.rememberShowCheck);
         this.rememberShowLabel = new Label();
         this.rememberShowLabel.autoSize = true;
         this.rememberShowLabel.debug = false;
         this.rememberShowLabel.font = Base.FONT_LIGHT;
         this.rememberShowLabel.color = 11776947;
         this.rememberShowLabel.$ = "extendedGUI.NewsWindow.newsDontShow";
         this.rememberShowLabel.size = 20;
         this.rememberShowBox.addChild(this.rememberShowLabel);
      }
      
      protected function onShowCheckChange(param1:Event) : void
      {
         Api.call(Api.SHOW_ME_NEWS_EVERYTIME,[{"flag":(!!this.rememberShowCheck.selected ? 0 : 1)}]);
      }
      
      override public function draw() : void
      {
         super.draw();
         this.rememberShowBox.setSize(width - sideMargin,this.rememberShowBoxHeight);
         this.rememberShowBox.fixedWidth = width - sideMargin;
         this.rememberShowBox.y = height - buttonsBoxHeight - this.rememberShowBoxHeight;
         this.rememberShowBox.invalidate();
         message.setSize(width - sideMargin,height - buttonsBoxHeight - topMargin - bottomMargin);
         message.draw();
      }
   }
}

