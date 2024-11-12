import {ResolveFn} from '@angular/router';
import {SheepService} from "../services/sheep.service";
import {inject} from "@angular/core";
import {Sheep} from "../models/sheep";

export const sheepResolver: ResolveFn<Sheep|null> = (route, _) => {
  const service = inject(SheepService);
  const id = route.paramMap.get('id');
  if(!id) return null;
  return service.getById(Number.parseInt(id));
};
