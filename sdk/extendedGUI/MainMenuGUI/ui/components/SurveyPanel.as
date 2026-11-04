package ui.components
{
   import com.dvalimona.components.*;
   import com.greensock.*;
   import com.greensock.easing.*;
   import events.*;
   import flash.display.*;
   import flash.utils.*;
   import lang.*;
   
   public class SurveyPanel extends NewPanelWithIcon
   {
      internal var captionText:String = "";
      
      internal var descrText:String = "";
      
      internal var icon_path:String = "";
      
      internal var end_time:Number = 0;
      
      private var timeElapsed:LabelShadowed;
      
      protected var iconLoader:Loader = new Loader();
      
      public function SurveyPanel(parent:DisplayObjectContainer = null, xpos:Number = 0, ypos:Number = 0)
      {
         super(parent,xpos,ypos);
         this.visible = false;
         this.mouseEnabled = true;
         this.mouseChildren = true;
      }
      
      protected function updatePositions() : *
      {
      }
      
      override protected function addChildren() : void
      {
         super.addChildren();
         caption.labelUpColor = Style.GOLD_OVER;
         caption.labelOverColor = Style.GOLD_OVER;
         this.timeElapsed = new LabelShadowed(standartBox);
         this.timeElapsed.color = 12895428;
         this.timeElapsed.size = 17;
         this.timeElapsed.font = Base.lightFontName;
         this.timeElapsed.paddingTop = 0;
         this.timeElapsed.paddingLeft = 35;
         caption.align = Label.LEFT;
         this.updateView();
         setTimeout(this.updateView,500);
         setTimeout(this.updateView,1000);
         setTimeout(this.updateView,2000);
      }
      
      public function time_end(value:Number) : *
      {
         this.end_time = value;
         this.tuneHeight(true);
      }
      
      override protected function tuneHeight(flag:Boolean) : void
      {
         var th:Number = Math.max(standartIcon.y + standartIcon.height + 10,this.timeElapsed.y + this.timeElapsed.height + 10);
         var tw:Number = preferredWidth;
         description.paddingTop = 18;
         description.size = 20;
         if(minMode)
         {
            th = 128;
            description.size = 16;
            description.paddingTop = 5;
            tw = 128;
         }
         TweenMax.to(this,0.8,{
            "height":th,
            "width":tw,
            "ease":Expo.easeOut,
            "onComplete":initListeners
         });
      }
      
      private function getTimeToEnd(time:Number) : String
      {
         var days:uint = 0;
         var real_days:uint = 0;
         var hours:uint = 0;
         var minutes:uint = 0;
         var result:String = "";
         var timeToRemain:uint = time;
         real_days = timeToRemain / (60 * 60 * 24);
         days = Math.ceil(timeToRemain / (60 * 60 * 24));
         hours = timeToRemain / (60 * 60);
         minutes = timeToRemain / 60;
         if(real_days >= 1)
         {
            result = Locale.getById("extendedGUI.PremiumPanel.days") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + ": " + days;
         }
         else if(hours >= 1)
         {
            result = Locale.getById("extendedGUI.PremiumPanel.hours") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + ": " + hours;
         }
         else
         {
            result = Locale.getById("extendedGUI.PremiumPanel.minutes") + " " + Locale.getById("extendedGUI.PremiumPanel.toEnd") + ": " + minutes;
         }
         return result;
      }
      
      override public function updateView() : void
      {
         super.updateView();
         this.timeElapsed.text = this.getTimeToEnd(this.end_time);
         this.timeElapsed.visible = !minMode;
         this.draw();
      }
      
      override public function draw() : void
      {
         super.draw();
      }
   }
}

