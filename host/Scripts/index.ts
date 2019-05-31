import * as $ from "jquery";
import * as sc from "socketcore-client-typescript";

$(function () {
  const realtime = new sc.WorkflowClient("/realtime", {
    senderId: "PrioritizeAgile.Website"
  });

  realtime.run(() => {
    realtime.subscribe(function (message: any) {
      $("body").append($(`<h3>${message.Namespace}.${message.Type}: ${message.Data}</h3>`))
    });

    realtime.send(
      "HelloWorld",
      new sc.Message("HelloWorld", "Greeting")
    );
  });
});
